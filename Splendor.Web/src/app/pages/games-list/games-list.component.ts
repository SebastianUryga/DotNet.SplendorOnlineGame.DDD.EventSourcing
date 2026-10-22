import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { GameService } from '../../core/services/game.service';
import { GameSummary } from '../../models/game-view.model';
import { environment } from '../../../environments/environment';

@Component({
    selector: 'app-games-list',
    standalone: true,
    imports: [CommonModule, RouterModule, FormsModule],
    templateUrl: './games-list.component.html',
    styleUrls: ['./games-list.component.css']
})
export class GamesListComponent implements OnInit, OnDestroy {
    games: GameSummary[] = [];
    newGameName = '';
    turnDurationSeconds = 60;
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
        this.gameService.createGame(this.newGameName.trim() || undefined, this.turnDurationSeconds).subscribe(res => {
            this.router.navigate(['/games', res.id, 'lobby']);
        });
    }

    // The creator is not a player, so the bot (invited once) takes both seats and the creator only watches
    watchBotMatch(): void {
        this.gameService.createGame(this.botMatchName()).subscribe(res => {
            this.gameService.invitePlayer(res.id, { inviteeId: environment.botUserId }).subscribe(() => {
                this.router.navigate(['/games', res.id, 'lobby']);
            });
        });
    }

    hasBotMatch(): boolean {
        const name = this.botMatchName();
        return this.games.some(g => g.name === name && g.status !== 'Finished');
    }

    private botMatchName(): string {
        return `${localStorage.getItem('splendor_guest_name') ?? 'Player'} bot vs bot`.slice(0, 40);
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
