import { Component, OnInit, OnDestroy, AfterViewChecked, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { GameService } from '../../core/services/game.service';
import { GameView, PlayerView } from '../../models/game-view.model';
import { NobleView } from '../../models/noble.model';
import { GemCollection, EMPTY_GEMS } from '../../models/gem-collection.model';
import { Card } from '../../models/card.model';
import { interval, Subscription, startWith } from 'rxjs';
import { SignalRService, GameNotification } from '../../core/services/signalr.service';
import { ToastService } from '../../core/services/toast.service';
import { GameAnimationService } from '../../core/services/game-animation.service';
import { ChooseNobleRequest } from '../../models/requests.model';
import { CardComponent } from './card/card.component';



@Component({
  selector: 'app-game',
  standalone: true,
  imports: [CommonModule, RouterModule, CardComponent],
  templateUrl: './game.component.html',
  styleUrls: ['./game.component.css']
})
export class GameComponent implements OnInit, OnDestroy, AfterViewChecked {
  private static readonly notificationTexts: Record<string, string> = {
    gems_taken: 'took gems',
    card_purchased: 'bought a card',
    card_reserved: 'reserved a card',
    noble_acquired: 'got a noble',
    turn_expired: 'ran out of time'
  };

  gameId!: string;
  game: GameView | null = null;
  selectedGems: any = { diamond: 0, sapphire: 0, emerald: 0, ruby: 0, onyx: 0 };
  gemTypes = ['diamond', 'sapphire', 'emerald', 'ruby', 'onyx', 'gold'];
  gemTypesExcludeGold = ['diamond', 'sapphire', 'emerald', 'ruby', 'onyx'];
  turnSecondsRemaining = 0;
  private signalrSubscription?: Subscription;
  private clockSubscription?: Subscription;
  selectedCardId?: string | null = null;
  selectedCard?: Card | undefined;
  entering = new Set<string>();
  private recentReserved = new Map<string, string>();

  constructor(
    private route: ActivatedRoute,
    private gameService: GameService,
    private signalRService: SignalRService,
    private toastService: ToastService,
    public anim: GameAnimationService
  ) { }

  @HostListener('document:keydown.escape')
  skipAnimations(): void {
    this.anim.skip();
  }

  ngAfterViewChecked(): void {
    if (!this.game) return;
    const g = this.game;
    const reserved = g.players.map(p => (p.reservedCardIds ?? []).join(',')).join('|');
    this.anim.snapshot([g.market1, g.market2, g.market3, g.nobles ?? []].map(a => a.join(',')).join('|') + reserved);
  }

  private latest: GameView | null = null;
  private pending: GameView | null = null;

  // Every incoming state goes through here. Diffs (new market cards, new reservations) run on arrival because
  // notifications are handled right after; rendering waits until running animations finish.
  private receive(view: GameView): void {
    const prev = this.latest;
    if (prev) {
      const old = new Set([...prev.market1, ...prev.market2, ...prev.market3]);
      [...view.market1, ...view.market2, ...view.market3].filter(id => !old.has(id)).forEach(id => this.entering.add(id));
      for (const p of view.players) {
        const was = new Set(prev.players.find(x => x.id === p.id)?.reservedCardIds ?? []);
        const added = (p.reservedCardIds ?? []).find(id => !was.has(id));
        if (added) this.recentReserved.set(p.id, added);
      }
    }
    this.latest = this.pending = view;
    Promise.resolve().then(() => this.anim.whenIdle()).then(() => {
      if (this.pending) this.game = this.pending;
      this.pending = null;
    });
  }

  getCardLevel(id: string): number {
    return this.gameService.getCard(id)?.level ?? 1;
  }

  private animate(n: GameNotification): void {
    const p = n.playerId;
    if (!p) return;
    const d = n.data ?? {};
    switch (n.type) {
      case 'gems_taken':
        this.anim.gems(d.gems as any, 'market', p);
        break;
      case 'card_purchased': {
        this.anim.gems(d.paidGems as any, 'player', p);
        if (d.cardId) this.anim.card('card:' + d.cardId, p);
        break;
      }
      case 'card_reserved': { // notification has no cardId by design; the view diff tells which card it was
        const id = this.recentReserved.get(p);
        if (id) this.anim.card(this.anim.has('card:' + id) ? 'card:' + id : 'deck:' + this.getCardLevel(id), p);
        break;
      }
      case 'noble_acquired':
        if (d.nobleId) this.anim.card('noble:' + d.nobleId, p);
        break;
    }
  }

  async ngOnInit(): Promise<void> {
    this.gameId = this.route.snapshot.paramMap.get('id')!;

    // Load first: a slow or failing SignalR connection must not leave the view empty
    this.refresh();
    this.gameService.getCards().subscribe();
    this.gameService.getNobles().subscribe();

    // Listen for updates
    this.signalrSubscription = this.signalRService.gameUpdated$
      .subscribe(gameView => {
        this.receive(gameView);
        this.gameService.updateGameCache(gameView);
      });
    this.signalrSubscription.add(this.signalRService.notifications$.subscribe(n => {
      this.animate(n);
      const text = GameComponent.notificationTexts[n.type];
      const player = this.game?.players.find(p => p.id === n.playerId);
      if (text && player && player.id !== this.getCurrentPlayer()?.id) {
        const gems = Object.entries(n.data?.gems ?? {}).filter(([, c]) => c > 0)
          .map(([g, c]) => c > 1 ? `${c} ${g}` : g).join(', ');
        this.toastService.show(`${player.name} ${text}${gems ? ': ' + gems : ''}`);
      }
    }));
    this.clockSubscription = interval(1000).pipe(startWith(0)).subscribe(() => {
      const expiresAt = this.game?.expiresAt;
      this.turnSecondsRemaining = expiresAt
        ? Math.max(0, Math.ceil((new Date(expiresAt).getTime() - Date.now()) / 1000))
        : 0;
    });

    await this.signalRService.connect();
    await this.signalRService.joinGame(this.gameId);
  }

  viewCard(cardId: string): void {
    this.selectedCardId = cardId;
    this.selectedCard = this.gameService.getCard(cardId);
    // If card not in cache, you may call gameService.getCards() to preload, but keep simple here
  }

  canBuyCard(cardId: string): boolean {
    const player = this.getCurrentPlayer();
    if (!player) return false;
    if (player.id !== this.game?.currentPlayerId) return false;
    if (this.game?.isGemReturnPending) return false;

    const card = this.gameService.getCard(cardId);
    if (!card) return false;

    // build bonuses from owned cards
    const bonuses = {
      diamond: this.getOwnedBonusCount(player, 'diamond'),
      sapphire: this.getOwnedBonusCount(player, 'sapphire'),
      emerald: this.getOwnedBonusCount(player, 'emerald'),
      ruby: this.getOwnedBonusCount(player, 'ruby'),
      onyx: this.getOwnedBonusCount(player, 'onyx'),
      gold: 0
    };

    const cost = this.getCardCost(cardId);
    const effective = {
      diamond: Math.max(0, (cost as any).diamond - (bonuses.diamond || 0)),
      sapphire: Math.max(0, (cost as any).sapphire - (bonuses.sapphire || 0)),
      emerald: Math.max(0, (cost as any).emerald - (bonuses.emerald || 0)),
      ruby: Math.max(0, (cost as any).ruby - (bonuses.ruby || 0)),
      onyx: Math.max(0, (cost as any).onyx - (bonuses.onyx || 0)),
      gold: 0
    };

    const deficit = this.gemTypesExcludeGold
      .map((c: any) => Math.max(0, (effective as any)[c] - (this.getPlayerGemCount(player, c) || 0)))
      .reduce((a: number, b: number) => a + b, 0);

    return deficit <= (this.getPlayerGemCount(player, 'gold') || 0);
  }

  ngOnDestroy(): void {
    this.signalrSubscription?.unsubscribe();
    this.clockSubscription?.unsubscribe();
    this.signalRService.leaveGame(this.gameId);
  }


  getCurrentPlayerName(): string {
    const p = this.game?.players.find(x => x.id === this.game?.currentPlayerId);
    return p ? p.name : 'Unknown';
  }

  getMarketGemCount(type: string): number {
    return (this.game?.marketGems as any)[type] || 0;
  }

  addToSelection(type: string): void {
    if (type === 'gold') return;
    this.selectedGems[type]++;
  }

  removeFromSelection(type: string): void {
    if (this.selectedGems[type] > 0) this.selectedGems[type]--;
  }

  resetSelection(): void {
    this.selectedGems = { diamond: 0, sapphire: 0, emerald: 0, ruby: 0, onyx: 0 };
  }

  canTakeGems(): boolean {
    const counts = Object.values(this.selectedGems) as number[];
    const total = counts.reduce((a, b) => a + b, 0);
    const distinct = this.gemTypesExcludeGold.filter(t => this.selectedGems[t] > 0).length;

    // Rule 1: up to 3 gems, all different colors (fewer than 3 is allowed)
    if (total >= 1 && total <= 3 && distinct === total) return true;

    // Rule 2: 2 same gems (if >= 4 available in market)
    const doubleType = this.gemTypesExcludeGold.find(t => this.selectedGems[t] === 2);
    if (doubleType && total === 2) {
      return this.getMarketGemCount(doubleType) >= 4;
    }

    return false;
  }

  takeGems(): void {
    const req = {
      playerId: this.game?.currentPlayerId || '',
      ...this.selectedGems,
      gold: 0
    };
    this.gameService.takeGems(this.gameId, req).subscribe(() => {
      this.resetSelection();
    });
  }

  getMarketCards(level: number): string[] {
    if (level === 1) return this.game?.market1 || [];
    if (level === 2) return this.game?.market2 || [];
    if (level === 3) return this.game?.market3 || [];
    return [];
  }

  getDeckCount(level: number): number {
    if (level === 1) return this.game?.deck1Count || 0;
    if (level === 2) return this.game?.deck2Count || 0;
    if (level === 3) return this.game?.deck3Count || 0;
    return 0;
  }

  // Nobles helpers
  getAvailableNobles(): NobleView[] {
    const ids = this.game?.nobles || [];
    const owned = new Set<string>((this.game?.players || []).flatMap(p => p.ownedNobleIds || []));
    return ids
      .map(id => this.gameService.getNoble(id))
      .filter((n): n is NobleView => !!n && !owned.has(n.id));
  }

  getNoblesForPlayer(p: PlayerView): NobleView[] {
    const ids = p.ownedNobleIds || [];
    return ids.map(id => this.gameService.getNoble(id)).filter((n): n is NobleView => !!n);
  }

  getNobleRequirementValue(nobleId: string, gemType: string): number {
    const noble = this.gameService.getNoble(nobleId);
    if (!noble || !noble.requirements) return 0;
    return (noble.requirements as any)[gemType] || 0;
  }

  getCardPoints(id: string): number {
    const card = this.gameService.getCard(id);
    return (card as any)?.prestigePoints ?? (card as any)?.points ?? 0;
  }
  getCardBonus(id: string): string {
    const card = this.gameService.getCard(id);
    const bonus = (card as any)?.bonusType;
    if (bonus === undefined || bonus === null) return '';
    const gemNames = ['diamond', 'sapphire', 'emerald', 'ruby', 'onyx', 'gold'];
    return typeof bonus === 'number' ? gemNames[bonus] : bonus.toLowerCase();
  }
  getCardCost(id: string): any {
    const card = this.gameService.getCard(id);
    return card?.cost || EMPTY_GEMS;
  }


  getPlayerGemCount(p: PlayerView, type: string): number {
    return (p.gems as any)[type] || 0;
  }

  getPlayerTotalGems(p?: PlayerView | null): number {
    if (!p) return 0;
    return this.gemTypes.reduce((sum, t) => sum + (this.getPlayerGemCount(p, t) || 0), 0);
  }

  getCurrentPlayer(): PlayerView | undefined {
    return this.game?.players.find(p => p.id === this.game?.currentPlayerId);
  }

  getOverlimitPlayer(): PlayerView | undefined {
    return this.game?.players.find(p => this.getPlayerTotalGems(p) > 10);
  }

  getCardCostValue(cardId: string, gemType: string): number {
    const cost = this.getCardCost(cardId);
    return cost ? (cost as any)[gemType] : 0;
  }

  resolveGemLimit(): void {
    const req = {
      playerId: this.game?.currentPlayerId || '',
      ...this.selectedGems,
      gold: 0
    };
    this.gameService.resolveGemLimit(this.gameId, req).subscribe(() => {
      this.resetSelection();
    });
  }

  chooseNoble(nobleId: string): void {
    const req: ChooseNobleRequest = {
      playerId: this.game?.currentPlayerId || '',
      nobleId: nobleId
    };
    this.gameService.chooseNoble(this.gameId, req).subscribe();
  }

  calculatePoints(p: PlayerView): number {
    return p.ownedCardIds.reduce((sum, id) => sum + this.getCardPoints(id), 0) + this.getNoblesForPlayer(p).reduce((sum, e) => sum + e.prestigePoints, 0);
  }

  // Count how many owned cards give a bonus of the specified gem type
  getOwnedBonusCount(p: PlayerView, gemType: string): number {
    if (!p || !p.ownedCardIds || p.ownedCardIds.length === 0) return 0;
    return p.ownedCardIds.reduce((acc, id) => {
      return acc + (this.getCardBonus(id) === gemType ? 1 : 0);
    }, 0);
  }

  buyCard(cardId: string): void {
    const req = {
      playerId: this.game?.currentPlayerId || '',
      cardId: cardId
    };
    this.gameService.buyCard(this.gameId, req).subscribe();
  }

  reserveCard(cardId: string | null, level: number | null = null): void {
    const req = {
      playerId: this.game?.currentPlayerId || '',
      cardId,
      level
    };
    this.gameService.reserveCard(this.gameId, req).subscribe();
  }

  refresh(): void {
    this.gameService.getGame(this.gameId).subscribe(game => this.receive(game));
  }
}
