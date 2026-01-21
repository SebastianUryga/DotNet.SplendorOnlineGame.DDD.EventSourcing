import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { GameService } from '../../core/services/game.service';
import { GameSummary } from '../../models/game-view.model';

@Component({
    selector: 'app-games-list',
    standalone: true,
    imports: [CommonModule, RouterModule],
    template: `
    <div class="container">
      <h1>Splendor - Games</h1>
      <button (click)="createNewGame()" class="btn-primary">New Game</button>
      
      <div class="games-grid">
        <div *ngFor="let game of games" class="game-card">
          <h3>Game {{ game.id.substring(0, 8) }}</h3>
          <p>Status: <strong>{{ game.status }}</strong></p>
          <p>Players: {{ game.playerCount }}</p>
          <button (click)="goToGame(game)" class="btn-secondary">
            {{ game.status === 'Created' ? 'Join / Lobby' : 'Open Game' }}
          </button>
        </div>
      </div>
      
      <div *ngIf="games.length === 0" class="no-games">
        No active games found. Create one to start!
      </div>
    </div>
  `,
    styles: [`
    .container { padding: 20px; max-width: 1000px; margin: 0 auto; color: #eee; }
    h1 { color: #fff; text-shadow: 0 0 10px rgba(255,255,255,0.3); }
    .games-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 20px; margin-top: 20px; }
    .game-card { 
      background: rgba(255, 255, 255, 0.05); 
      border: 1px solid rgba(255, 255, 255, 0.1); 
      padding: 20px; 
      border-radius: 12px;
      backdrop-filter: blur(5px);
      transition: transform 0.2s;
    }
    .game-card:hover { transform: translateY(-5px); background: rgba(255, 255, 255, 0.08); }
    .btn-primary { 
      background: #4a90e2; color: white; border: none; padding: 10px 20px; 
      border-radius: 6px; cursor: pointer; font-weight: bold;
    }
    .btn-secondary { 
      background: rgba(255,255,255,0.1); color: white; border: 1px solid rgba(255,255,255,0.2); 
      padding: 8px 16px; border-radius: 6px; cursor: pointer; width: 100%; margin-top: 10px;
    }
    .no-games { margin-top: 40px; text-align: center; opacity: 0.6; }
  `]
})
export class GamesListComponent implements OnInit {
    games: GameSummary[] = [];

    constructor(private gameService: GameService, private router: Router) { }

    ngOnInit(): void {
        this.refreshGames();
    }

    refreshGames(): void {
        this.gameService.getGames().subscribe(games => this.games = games);
    }

    createNewGame(): void {
        this.gameService.createGame().subscribe(res => {
            this.router.navigate(['/games', res.id, 'lobby']);
        });
    }

    goToGame(game: GameSummary): void {
        if (game.status === 'Created') {
            this.router.navigate(['/games', game.id, 'lobby']);
        } else {
            this.router.navigate(['/games', game.id, 'play']);
        }
    }
}
