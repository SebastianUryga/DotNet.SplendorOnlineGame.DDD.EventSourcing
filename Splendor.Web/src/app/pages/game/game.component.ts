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
  templateUrl: './game.component.html',
  styleUrls: ['./game.component.css']
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
        this.gameService.updateGameCache(gameView);
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
