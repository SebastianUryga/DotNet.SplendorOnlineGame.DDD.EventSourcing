import { GemCollection } from './gem-collection.model';

export interface Card {
    id: string;
    level: number;
    bonusType: string;
    prestigePoints: number;
    cost: GemCollection;
}
