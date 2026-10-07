import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface Toast { id: number; text: string; }

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  readonly toasts$ = new BehaviorSubject<Toast[]>([]);

  show(text: string, ms = 4000): void {
    const id = this.nextId++;
    this.toasts$.next([...this.toasts$.value, { id, text }]);
    setTimeout(() => this.toasts$.next(this.toasts$.value.filter(t => t.id !== id)), ms);
  }
}
