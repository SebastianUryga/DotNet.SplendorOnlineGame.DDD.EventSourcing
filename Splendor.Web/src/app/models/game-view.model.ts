import { GemCollection } from './gem-collection.model';

export interface PlayerView {
    id: string;
    ownerId: string;
    name: string;
    gems: GemCollection;
    ownedCardIds: string[];
}

export interface GameView {
    id: string;
    version: number;
    status: string;
    players: PlayerView[];
    marketGems: GemCollection;
    currentPlayerId: string | null;
    market1: string[];
    market2: string[];
    market3: string[];
    deck1Count: number;
    deck2Count: number;
    deck3Count: number;
}

export interface GameSummary {
    id: string;
    status: string;
    playerCount: number;
}
