import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { GameService } from '../../core/services/game.service';
import { SignalRService } from '../../core/services/signalr.service';
import { GameView } from '../../models/game-view.model';

@Component({
    selector: 'app-lobby',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule],
    template: `
    <div class="container" *ngIf="game">
      <h1>Game Lobby</h1>
      <div class="info">
        <p>Game ID: <span>{{ game.id }}</span></p>
        <p>Status: <span class="status">{{ game.status }}</span></p>
      </div>

      <div class="players-list">
        <h3>Players ({{ game.players.length }})</h3>
        <div *ngIf="game.players.length === 0" class="empty">No players yet.</div>
        <ul>
          <li *ngFor="let player of game.players">
            <span class="player-name">{{ player.name }}</span>
            <span class="player-id">{{ player.id }}</span>
          </li>
        </ul>
      </div>

      <div *ngIf="!isJoined && game.status === 'Created'" class="join-form">
        <input [(ngModel)]="playerName" placeholder="Enter your player name" (keyup.enter)="join()" />
        <button (click)="join()" [disabled]="!playerName" class="btn-join">Join Game</button>
      </div>

      <div class="actions">
        <button (click)="start()" [disabled]="game.players.length < 2" *ngIf="game.status === 'Created'" class="btn-start">
          Start Game
        </button>
        <button [routerLink]="['/games']" class="btn-back">Back to List</button>
      </div>
    </div>
  `,
    styles: [`
    .container { padding: 20px; max-width: 600px; margin: 0 auto; color: #eee; }
    .info { margin-bottom: 20px; }
    .info span { font-family: monospace; color: #4a90e2; }
    .status { font-weight: bold; text-transform: uppercase; }
    .players-list { margin: 20px 0; background: rgba(255,255,255,0.05); padding: 20px; border-radius: 12px; border: 1px solid rgba(255,255,255,0.1); }
    ul { list-style: none; padding: 0; margin: 15px 0 0 0; }
    li { padding: 10px; border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; justify-content: space-between; align-items: center; }
    li:last-child { border-bottom: none; }
    .player-name { font-weight: bold; }
    .player-id { font-size: 0.8em; opacity: 0.5; font-family: monospace; }
    .join-form { margin: 30px 0; display: flex; gap: 10px; }
    input { flex: 1; padding: 12px; border-radius: 8px; border: 1px solid #444; background: #111; color: #fff; }
    button { padding: 12px 24px; border-radius: 8px; cursor: pointer; border: none; font-weight: bold; transition: all 0.2s; }
    .btn-join { background: #4a90e2; color: white; }
    .btn-join:hover:not(:disabled) { background: #357abd; }
    .btn-start { background: #27ae60; color: white; }
    .btn-start:hover:not(:disabled) { background: #219150; }
    .btn-start:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-back { background: rgba(255,255,255,0.1); color: white; border: 1px solid rgba(255,255,255,0.2); }
    .actions { display: flex; gap: 15px; margin-top: 20px; }
    .empty { opacity: 0.5; font-style: italic; }
  `]
})
export class LobbyComponent implements OnInit, OnDestroy {
    gameId!: string;
    game: GameView | null = null;
    playerName: string = '';
    isJoined: boolean = false;
    private signalrSubscription?: Subscription;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private gameService: GameService,
        private signalRService: SignalRService
    ) { }

    async ngOnInit(): Promise<void> {
        this.gameId = this.route.snapshot.paramMap.get('id')!;

        await this.signalRService.connect();
        await this.signalRService.joinGame(this.gameId);

        this.signalrSubscription = this.signalRService.gameUpdated$
            .subscribe(gameView => {
                this.game = gameView;
                if (gameView.status === 'Started') {
                    this.router.navigate(['/games', this.gameId, 'play']);
                }
            });

        this.refresh();
    }

    ngOnDestroy(): void {
        this.signalrSubscription?.unsubscribe();
        this.signalRService.leaveGame(this.gameId);
    }

    join(): void {
        if (!this.playerName) return;
        this.gameService.joinGame(this.gameId, this.playerName).subscribe(() => {
            this.isJoined = true;
            this.refresh();
        });
    }

    start(): void {
        this.gameService.startGame(this.gameId).subscribe(() => {
            this.router.navigate(['/games', this.gameId, 'play']);
        });
    }

    refresh(): void {
        this.gameService.getGame(this.gameId).subscribe(game => this.game = game);
    }
}
