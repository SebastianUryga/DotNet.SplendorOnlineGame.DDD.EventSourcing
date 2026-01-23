import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GameView } from '../../models/game-view.model';

@Injectable({
    providedIn: 'root'
})
export class SignalRService {
    private hubConnection?: HubConnection;
    private gameUpdated = new Subject<GameView>();

    gameUpdated$ = this.gameUpdated.asObservable();

    async connect(): Promise<void> {
        if (this.hubConnection) return;

        this.hubConnection = new HubConnectionBuilder()
            .withUrl(`${environment.apiUrl}/hubs/game`)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        this.hubConnection.on('GameUpdated', (gameView: GameView) => {
            this.gameUpdated.next(gameView);
        });

        await this.hubConnection.start();
    }

    async joinGame(gameId: string): Promise<void> {
        if (this.hubConnection) {
            await this.hubConnection.invoke('JoinGame', gameId);
        }
    }

    async leaveGame(gameId: string): Promise<void> {
        if (this.hubConnection) {
            await this.hubConnection.invoke('LeaveGame', gameId);
        }
    }

    async disconnect(): Promise<void> {
        if (this.hubConnection) {
            await this.hubConnection.stop();
            this.hubConnection = undefined;
        }
    }
}
