import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { GameService } from '../../core/services/game.service';
import { GameSummary } from '../../models/game-view.model';

@Component({
    selector: 'app-games-list',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './games-list.component.html',
    styleUrls: ['./games-list.component.css']
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

    deleteGame(game: GameSummary, event: Event): void {
        event.stopPropagation();
        this.gameService.deleteGame(game.id).subscribe({
            next: () => {
                this.games = this.games.filter(g => g.id !== game.id);
            },
            error: (err) => {
                console.error('Failed to delete game:', err);
                this.refreshGames();
            }
        });
    }
}
