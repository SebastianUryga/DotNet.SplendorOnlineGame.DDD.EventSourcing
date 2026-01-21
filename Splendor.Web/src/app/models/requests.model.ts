import { GemCollection } from './gem-collection.model';

export interface JoinGameRequest {
    name: string;
}

export interface TakeGemsRequest {
    playerId: string;
    diamond: number;
    sapphire: number;
    emerald: number;
    ruby: number;
    onyx: number;
    gold: number;
}

export interface BuyCardRequest {
    playerId: string;
    cardId: string;
}
