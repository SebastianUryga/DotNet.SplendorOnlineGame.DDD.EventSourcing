import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap, of } from 'rxjs';
import { GameSummary, GameView } from '../../models/game-view.model';
import { BuyCardRequest, JoinGameRequest, TakeGemsRequest } from '../../models/requests.model';
import { environment } from '../../../environments/environment';
import { Card } from '../../models/card.model';


@Injectable({
    providedIn: 'root'
})
export class GameService {
    private gamesUrl = `${environment.apiUrl}/games`;
    private cardsUrl = `${environment.apiUrl}/cards`;

    // Cache for card definitions
    private cardsMap: Record<string, Card> = {};

    constructor(private http: HttpClient) { }

    getCards(): Observable<Card[]> {
        return this.http.get<Card[]>(this.cardsUrl).pipe(
            tap(cards => {
                cards.forEach(c => this.cardsMap[c.id] = c);
            })
        );
    }

    getCard(id: string): Card | undefined {
        return this.cardsMap[id];
    }

    getGames(): Observable<GameSummary[]> {
        return this.http.get<GameSummary[]>(this.gamesUrl);
    }

    createGame(): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(this.gamesUrl, {});
    }

    getGame(id: string): Observable<GameView> {
        return this.http.get<GameView>(`${this.gamesUrl}/${id}`);
    }

    joinGame(id: string, name: string): Observable<void> {
        return this.http.post<void>(`${this.gamesUrl}/${id}/players`, { name });
    }

    startGame(id: string): Observable<void> {
        return this.http.post<void>(`${this.gamesUrl}/${id}/start`, {});
    }

    takeGems(id: string, request: TakeGemsRequest): Observable<void> {
        return this.http.post<void>(`${this.gamesUrl}/${id}/actions/take-gems`, request);
    }

    buyCard(id: string, request: BuyCardRequest): Observable<void> {
        return this.http.post<void>(`${this.gamesUrl}/${id}/actions/buy-card`, request);
    }

    getVersion(id: string): Observable<number> {
        return this.http.get<any>(`${this.gamesUrl}/${id}/version`).pipe(
            map(res => res.version ?? res.Version)
        );
    }
}
