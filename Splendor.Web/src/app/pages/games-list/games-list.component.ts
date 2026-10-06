import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
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
export class GamesListComponent implements OnInit, OnDestroy {
    games: GameSummary[] = [];
    private tokenSubscription?: Subscription;

    constructor(private gameService: GameService, private router: Router, private authService: AuthService) { }

    ngOnInit(): void {
        // reload whenever the token changes (guest login, pasted token)
        this.tokenSubscription = this.authService.token$.subscribe(token => {
            if (token) this.refreshGames();
        });
    }

    ngOnDestroy(): void {
        this.tokenSubscription?.unsubscribe();
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
