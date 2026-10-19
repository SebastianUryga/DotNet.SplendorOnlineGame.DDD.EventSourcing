import { Injectable, NgZone } from '@angular/core';
const KEY = 'splendor.animations';
const GEM_COLORS: Record<string, string> = {
  diamond: '#fff', sapphire: '#4a90e2', emerald: '#2ecc71', ruby: '#e74c3c', onyx: '#333', gold: '#f1c40f'
};
const DOT_GAP_MS = 130;
const DOT_MS = 800;
const CARD_MS = 1200;
const MAX_DOTS = 12;

const attr = (s: string) => s.replace(/["\\]/g, '\\$&');

interface Snap { rect: DOMRect; node: HTMLElement; }

// DOM-only animations (fly card / gem dots). Looks elements up by data-* attributes set in the game template.
@Injectable({ providedIn: 'root' })
export class GameAnimationService {
  enabled = !window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
  private active = new Set<Animation>();
  private waiters: (() => void)[] = [];
  private key = '';
  private prev = new Map<string, Snap>();
  private cur = new Map<string, Snap>();

  constructor(private zone: NgZone) {
    try {
      const saved = localStorage.getItem(KEY);
      if (saved !== null) this.enabled = saved === '1';
    } catch { /* storage unavailable: keep default */ }
  }

  get running(): number { return this.active.size; }

  // Resolves once no animation is running (immediately when idle).
  whenIdle(): Promise<void> {
    return this.active.size ? new Promise(resolve => this.waiters.push(resolve)) : Promise.resolve();
  }

  toggle(): void {
    this.enabled = !this.enabled;
    try { localStorage.setItem(KEY, this.enabled ? '1' : '0'); } catch { /* ignore */ }
    if (!this.enabled) this.skip();
  }

  // Jump every running animation (ghosts and CSS entrances) to its end state.
  skip(): void {
    [...this.active].forEach(a => { try { a.finish(); } catch { /* already done */ } });
    document.getAnimations().forEach(a => {
      if ((a as any).animationName) { try { a.finish(); } catch { /* infinite */ } }
    });
  }

  // Remember how data-snap elements looked in the last rendered state: the source may be gone by the time a
  // notification is handled (HTTP refresh can render the new state before SignalR delivers the event).
  snapshot(key: string): void {
    if (key === this.key) return;
    this.key = key;
    this.prev = this.cur;
    this.cur = new Map();
    document.querySelectorAll<HTMLElement>('[data-snap]').forEach(e => {
      const node = e.cloneNode(true) as HTMLElement;
      node.removeAttribute('data-snap');
      this.cur.set(e.dataset['snap']!, { rect: e.getBoundingClientRect(), node });
    });
  }

  has(snapKey: string): boolean {
    return !!this.source(snapKey);
  }

  // Card/noble/deck-top ghost flies to the player's panel.
  card(snapKey: string, playerId: string): void {
    const to = this.panel(playerId);
    const src = this.source(snapKey);
    if (!this.ok() || !to || !src) return;
    this.fly(src.node, src.rect, to.getBoundingClientRect(), 0, CARD_MS, 0.3);
  }

  // One dot per gem: market -> player (taken) or player -> market (paid).
  gems(gems: Record<string, number> | undefined, from: 'market' | 'player', playerId: string): void {
    if (!gems || !this.ok()) return;
    let i = 0;
    for (const type of Object.keys(GEM_COLORS)) {
      const market = document.querySelector<HTMLElement>(`[data-market-gem="${type}"]`);
      const own = document.querySelector<HTMLElement>(`[data-player-id="${attr(playerId)}"] [data-player-gem="${type}"]`);
      const [a, b] = from === 'market' ? [market, own] : [own, market];
      for (let k = 0; a && b && k < (gems[type] ?? 0) && i < MAX_DOTS; k++, i++) {
        const dot = document.createElement('div');
        dot.style.cssText = `border-radius:50%;border:2px solid #fff;background:${GEM_COLORS[type]};box-shadow:0 2px 6px #000`;
        this.fly(dot, this.dotRect(a), this.dotRect(b), i * DOT_GAP_MS, DOT_MS, 1, () => this.pulse(b));
      }
    }
  }

  private ok(): boolean { return this.enabled && !document.hidden; }

  private source(snapKey: string): Snap | undefined {
    const live = document.querySelector<HTMLElement>(`[data-snap="${attr(snapKey)}"]`);
    if (live) {
      const node = live.cloneNode(true) as HTMLElement;
      node.removeAttribute('data-snap');
      return { rect: live.getBoundingClientRect(), node };
    }
    const s = this.prev.get(snapKey) ?? this.cur.get(snapKey);
    return s && { rect: s.rect, node: s.node.cloneNode(true) as HTMLElement };
  }

  private panel(playerId: string): HTMLElement | null {
    return document.querySelector<HTMLElement>(`[data-player-id="${attr(playerId)}"]`);
  }

  private dotRect(e: HTMLElement): DOMRect {
    const r = e.getBoundingClientRect(), s = 16;
    return new DOMRect(r.left + r.width / 2 - s / 2, r.top + r.height / 2 - s / 2, s, s);
  }

  private pulse(e: HTMLElement): void {
    e.animate([{ transform: 'scale(1)' }, { transform: 'scale(1.3)' }, { transform: 'scale(1)' }], 400);
  }

  private fly(el: HTMLElement, from: DOMRect, to: DOMRect, delay: number, ms: number, endScale: number, onEnd?: () => void): void {
    Object.assign(el.style, {
      position: 'fixed', left: `${from.left}px`, top: `${from.top}px`, width: `${from.width}px`,
      height: `${from.height}px`, margin: '0', zIndex: '1000', pointerEvents: 'none'
    });
    document.body.appendChild(el);
    const dx = to.left + to.width / 2 - (from.left + from.width / 2);
    const dy = to.top + to.height / 2 - (from.top + from.height / 2);
    const a = el.animate(
      [{ transform: 'none', opacity: 1 }, { transform: `translate(${dx}px, ${dy}px) scale(${endScale})`, opacity: 0.85 }],
      { duration: ms, delay, easing: 'ease-in-out', fill: 'both' });
    this.active.add(a);
    const done = () => this.zone.run(() => {
      this.active.delete(a);
      el.remove();
      onEnd?.();
      if (!this.active.size) this.waiters.splice(0).forEach(resolve => resolve());
    });
    a.onfinish = done;
    a.oncancel = done;
  }
}
