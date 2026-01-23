import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { GameService } from '../../core/services/game.service';
import { GameView, PlayerView } from '../../models/game-view.model';
import { GemCollection, EMPTY_GEMS } from '../../models/gem-collection.model';
import { interval, Subscription, startWith, switchMap, filter, firstValueFrom } from 'rxjs';
import { SignalRService } from '../../core/services/signalr.service';



@Component({
  selector: 'app-game',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="game-container" *ngIf="game">
      <!-- Sidebar: Players -->
      <div class="players-sidebar">
        <h2 class="sidebar-title">Players</h2>
        <div *ngFor="let p of game.players" class="player-card" [class.active]="p.id === game.currentPlayerId">
          <div class="player-header">
            <h4>{{ p.name }}</h4>
            <span class="score">{{ calculatePoints(p) }} pts</span>
          </div>
          <div class="mini-gems">
            <div *ngFor="let g of gemTypes" class="mini-gem" [class]="g">
              {{ getPlayerGemCount(p, g) }}
            </div>
          </div>
          <div class="player-stats">
            <span>Cards: {{ p.ownedCardIds.length }}</span>
            <span class="active-badge" *ngIf="p.id === game.currentPlayerId">Current Turn</span>
          </div>
        </div>
        <button [routerLink]="['/games']" class="btn-quit">Quit Game</button>
      </div>

      <!-- Main: Market -->
      <div class="main-board">
        <div class="header">
          <h2>Splendor Table</h2>
          <div class="game-meta">
            <span>ID: {{ game.id.substring(0,8) }}</span>
            <span class="version-badge">Version {{ game.version }}</span>
          </div>
        </div>

        <!-- Gem Market -->
        <div class="gem-market">
          <h3>Gem Market</h3>
          <div class="gems-row">
            <div *ngFor="let g of gemTypes" class="market-gem">
              <div class="gem-token" [class]="g">
                <span class="market-count">{{ getMarketGemCount(g) }}</span>
              </div>
              <div class="selection-controls" *ngIf="g !== 'gold'">
                <button (click)="removeFromSelection(g)" [disabled]="selectedGems[g] <= 0">-</button>
                <span class="sel-count">{{ selectedGems[g] || 0 }}</span>
                <button (click)="addToSelection(g)" [disabled]="getMarketGemCount(g) <= selectedGems[g]">+</button>
              </div>
            </div>
          </div>
          <div class="market-actions">
            <button (click)="takeGems()" [disabled]="!canTakeGems()" class="btn-action">Take Selected Gems</button>
            <button (click)="resetSelection()" class="btn-reset">Reset</button>
          </div>
        </div>

        <!-- Card Market -->
        <div class="card-market">
          <div *ngFor="let level of [3, 2, 1]" class="market-level">
            <div class="level-header">
              <h4>Level {{ level }}</h4>
              <span class="deck-count">{{ getDeckCount(level) }} in deck</span>
            </div>
            <div class="cards-row">
              <div *ngFor="let cardId of getMarketCards(level)" class="card-item">
                <div class="card">
                  <div class="card-top">
                    <span class="points" *ngIf="getCardPoints(cardId) > 0">{{ getCardPoints(cardId) }}</span>
                    <div class="bonus-icon" [class]="getCardBonus(cardId)"></div>
                  </div>
                  <div class="card-cost">
                    <ng-container *ngFor="let c of gemTypesExcludeGold">
                      <div class="cost-token" *ngIf="getCardCostValue(cardId, c) > 0">
                        <span class="dot" [class]="c"></span>
                        {{ getCardCostValue(cardId, c) }}
                      </div>
                    </ng-container>
                  </div>
                </div>
                <button (click)="buyCard(cardId)" class="btn-buy">Purchase</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .game-container { display: flex; height: 100vh; background: #0f0f0f; color: #eee; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
    
    /* Sidebar */
    .players-sidebar { width: 300px; background: #1a1a1a; padding: 25px; border-right: 1px solid #333; display: flex; flex-direction: column; }
    .sidebar-title { margin-bottom: 25px; color: #fff; font-size: 1.5em; letter-spacing: 1px; }
    .player-card { background: #222; padding: 20px; border-radius: 15px; margin-bottom: 20px; border: 1px solid #333; transition: all 0.3s; }
    .player-card.active { border-color: #4a90e2; background: #2a2a2a; box-shadow: 0 0 20px rgba(74, 144, 226, 0.2); }
    .player-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
    .score { font-weight: bold; color: #f1c40f; font-size: 1.1em; }
    .mini-gems { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 12px; }
    .mini-gem { padding: 4px 10px; border-radius: 6px; font-size: 0.85em; font-weight: bold; min-width: 30px; text-align: center; }
    .player-stats { display: flex; justify-content: space-between; font-size: 0.8em; opacity: 0.7; align-items: center; }
    .active-badge { color: #4a90e2; font-weight: bold; text-transform: uppercase; letter-spacing: 1px; }
    .btn-quit { margin-top: auto; padding: 12px; background: transparent; border: 1px solid #e74c3c; color: #e74c3c; border-radius: 8px; cursor: pointer; }

    /* Main Board */
    .main-board { flex: 1; padding: 40px; overflow-y: auto; background: linear-gradient(135deg, #0f0f0f 0%, #1a1a1a 100%); }
    .header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 40px; }
    .version-badge { background: #333; padding: 4px 10px; border-radius: 15px; font-size: 0.8em; margin-left: 15px; color: #aaa; }
    
    /* Gem Market */
    .gem-market { background: rgba(255, 255, 255, 0.03); padding: 25px; border-radius: 20px; border: 1px solid rgba(255, 255, 255, 0.05); margin-bottom: 40px; }
    .gems-row { display: flex; gap: 30px; margin: 20px 0; }
    .market-gem { display: flex; flex-direction: column; align-items: center; gap: 15px; }
    .gem-token { width: 60px; height: 60px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 1.5em; font-weight: bold; box-shadow: 0 4px 10px rgba(0,0,0,0.5); position: relative; }
    .gem-token.diamond { background: radial-gradient(circle at 30% 30%, #fff 0%, #bdc3c7 100%); color: #2c3e50; }
    .gem-token.sapphire { background: radial-gradient(circle at 30% 30%, #4a90e2 0%, #2980b9 100%); }
    .gem-token.emerald { background: radial-gradient(circle at 30% 30%, #2ecc71 0%, #27ae60 100%); }
    .gem-token.ruby { background: radial-gradient(circle at 30% 30%, #e74c3c 0%, #c0392b 100%); }
    .gem-token.onyx { background: radial-gradient(circle at 30% 30%, #34495e 0%, #2c3e50 100%); }
    .gem-token.gold { background: radial-gradient(circle at 30% 30%, #f1c40f 0%, #f39c12 100%); color: #2c3e50; }
    .market-count { text-shadow: 0 1px 3px rgba(0,0,0,0.5); }
    .selection-controls { display: flex; align-items: center; gap: 10px; background: #000; padding: 4px 10px; border-radius: 20px; border: 1px solid #333; }
    .selection-controls button { background: none; border: none; color: #fff; font-size: 1.2em; cursor: pointer; padding: 0 5px; }
    .sel-count { min-width: 15px; text-align: center; font-weight: bold; color: #4a90e2; }
    .market-actions { display: flex; gap: 15px; }
    .btn-action { background: #4a90e2; color: white; border: none; padding: 12px 30px; border-radius: 10px; font-weight: bold; cursor: pointer; }
    .btn-action:disabled { opacity: 0.3; cursor: not-allowed; }
    .btn-reset { background: transparent; border: 1px solid #555; color: #aaa; padding: 12px 20px; border-radius: 10px; cursor: pointer; }

    /* Card Market */
    .market-level { margin-bottom: 40px; }
    .level-header { display: flex; align-items: baseline; gap: 15px; margin-bottom: 15px; }
    .deck-count { font-size: 0.85em; opacity: 0.5; }
    .cards-row { display: flex; gap: 20px; overflow-x: auto; padding-bottom: 20px; }
    .card-item { display: flex; flex-direction: column; gap: 10px; transition: transform 0.2s; }
    .card-item:hover { transform: scale(1.02); }
    .card { width: 120px; height: 160px; background: #fff; border-radius: 10px; padding: 10px; display: flex; flex-direction: column; border: 3px solid #ccc; color: #333; }
        .card-top { display: flex; justify-content: space-between; align-items: center; }
    .points { font-size: 1.6em; font-weight: bold; font-family: 'Georgia', serif; }
    .bonus-icon { width: 25px; height: 25px; border-radius: 6px; box-shadow: inset 0 2px 5px rgba(0,0,0,0.1); }
    .bonus-icon.diamond { background: #eee; border: 1px solid #ccc; }
    .bonus-icon.sapphire { background: #4a90e2; }
    .bonus-icon.emerald { background: #2ecc71; }
    .bonus-icon.ruby { background: #e74c3c; }
    .bonus-icon.onyx { background: #333; }
    
    .card-cost { margin-top: auto; display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
    .cost-token { display: flex; align-items: center; gap: 4px; font-weight: bold; font-size: 0.85em; }
    .dot { width: 12px; height: 12px; border-radius: 50%; display: inline-block; border: 1px solid rgba(0,0,0,0.1); }

    .btn-buy { background: #333; color: white; border: none; padding: 10px; border-radius: 8px; cursor: pointer; font-weight: bold; }
    
    /* Shared colors for mini-gems and dots */
    .diamond { background: #fff !important; color: #333 !important; }
    .sapphire { background: #4a90e2 !important; color: #fff !important; }
    .emerald { background: #2ecc71 !important; color: #fff !important; }
    .ruby { background: #e74c3c !important; color: #fff !important; }
    .onyx { background: #333 !important; color: #fff !important; }
    .gold { background: #f1c40f !important; color: #333 !important; }
  `]
})
export class GameComponent implements OnInit, OnDestroy {
  gameId!: string;
  game: GameView | null = null;
  selectedGems: any = { diamond: 0, sapphire: 0, emerald: 0, ruby: 0, onyx: 0 };
  gemTypes = ['diamond', 'sapphire', 'emerald', 'ruby', 'onyx', 'gold'];
  gemTypesExcludeGold = ['diamond', 'sapphire', 'emerald', 'ruby', 'onyx'];
  private signalrSubscription?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private gameService: GameService,
    private signalRService: SignalRService
  ) { }

  async ngOnInit(): Promise<void> {
    this.gameId = this.route.snapshot.paramMap.get('id')!;

    // Connect to SignalR
    await this.signalRService.connect();
    await this.signalRService.joinGame(this.gameId);

    // Listen for updates
    this.signalrSubscription = this.signalRService.gameUpdated$
      .subscribe(gameView => {
        this.game = gameView;
      });

    // Initial load
    this.refresh();
  }

  ngOnDestroy(): void {
    this.signalrSubscription?.unsubscribe();
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

    // Rule 1: 3 different gems
    if (total === 3 && distinct === 3) return true;

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
      this.refresh();
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

  getCardCostValue(cardId: string, gemType: string): number {
    const cost = this.getCardCost(cardId);
    return cost ? (cost as any)[gemType] : 0;
  }

  calculatePoints(p: PlayerView): number {
    return p.ownedCardIds.reduce((sum, id) => sum + this.getCardPoints(id), 0);
  }

  buyCard(cardId: string): void {
    const req = {
      playerId: this.game?.currentPlayerId || '',
      cardId: cardId
    };
    this.gameService.buyCard(this.gameId, req).subscribe(() => {
      this.refresh();
    });
  }

  refresh(): void {
    this.gameService.getGame(this.gameId).subscribe(game => this.game = game);
  }
}
