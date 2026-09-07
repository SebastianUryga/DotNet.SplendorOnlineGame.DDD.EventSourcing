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
    templateUrl: './lobby.component.html',
    styleUrls: ['./lobby.component.css']
})
export class LobbyComponent implements OnInit, OnDestroy {
  gameId!: string;
    game: GameView | null = null;
    playerName: string = '';
    isJoined: boolean = false;
    inviteeId: string = '';

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

  invite(): void {
    if (!this.inviteeId) return;
    this.gameService.invitePlayer(this.gameId, { inviteeId: this.inviteeId }).subscribe(() => {
      this.inviteeId = '';
      this.refresh();
    });
  }
}
