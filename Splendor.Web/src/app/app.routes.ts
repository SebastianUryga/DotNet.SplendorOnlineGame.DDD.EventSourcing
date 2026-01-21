import { Routes } from '@angular/router';
import { GamesListComponent } from './pages/games-list/games-list.component';
import { LobbyComponent } from './pages/lobby/lobby.component';
import { GameComponent } from './pages/game/game.component';

export const routes: Routes = [
    { path: '', redirectTo: '/games', pathMatch: 'full' },
    { path: 'games', component: GamesListComponent },
    { path: 'games/:id/lobby', component: LobbyComponent },
    { path: 'games/:id/play', component: GameComponent },
];
