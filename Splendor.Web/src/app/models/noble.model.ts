import { GemCollection } from './gem-collection.model';

export interface NobleView {
  id: string;
  // optional display name
  name?: string;
  prestigePoints: number;
  // requirements expressed as bonus counts (no gold)
  requirements: Omit<GemCollection, 'gold'>;
  ownerId?: string | null;
}
