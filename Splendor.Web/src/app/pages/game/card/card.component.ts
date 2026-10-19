import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

const COST_TYPES = ['diamond', 'sapphire', 'emerald', 'ruby', 'onyx'];

// Presentational card: art from assets/cards (CSS gradient shows until the image exists), overlay on top.
@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './card.component.html',
  styleUrls: ['./card.component.css']
})
export class CardComponent implements OnChanges {
  @Input() bonus = '';
  @Input() points = 0;
  @Input() cost: any = {};
  @Input() level = 1;
  @Input() size: 'mini' | 'normal' | 'lg' = 'normal';
  @Input() faceDown = false;

  img: string | null = null;
  costs: { type: string; n: number }[] = [];

  ngOnChanges(): void {
    const art = this.faceDown ? `card-back-${this.level}` : this.bonus ? `card-${this.bonus}-${this.level}` : '';
    this.img = art ? `url(assets/cards/${art}.webp)` : null;
    this.costs = COST_TYPES.map(type => ({ type, n: this.cost?.[type] ?? 0 })).filter(c => c.n > 0);
  }

  byType = (_: number, c: { type: string }) => c.type;
}
