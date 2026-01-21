import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <header class="app-header">
      <div class="logo">Splendor Online</div>
      <div class="auth-box">
        <input [(ngModel)]="token" placeholder="Paste JWT Token here..." class="token-input" (keyup.enter)="saveToken()" />
        <button (click)="saveToken()" class="btn-save">Set Token</button>
        <div class="status-indicator">
          <span class="status-dot" [class.valid]="hasToken"></span>
          <span class="status-text">{{ hasToken ? 'Authenticated' : 'No Token' }}</span>
        </div>
      </div>
    </header>
    
    <main class="content-area">
      <router-outlet></router-outlet>
    </main>

    <footer class="app-footer">
      Splendor DDD Event Sourcing Project &copy; 2026
    </footer>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: #0f0f0f; font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }
    .app-header { 
      background: rgba(0,0,0,0.8); backdrop-filter: blur(10px); padding: 15px 40px; 
      display: flex; justify-content: space-between; align-items: center; 
      border-bottom: 1px solid rgba(255,255,255,0.1); position: sticky; top: 0; z-index: 1000;
    }
    .logo { font-size: 1.4em; font-weight: 900; color: #fff; letter-spacing: 3px; font-style: italic; }
    .auth-box { display: flex; align-items: center; gap: 20px; }
    .token-input { background: #111; border: 1px solid #333; color: #4a90e2; padding: 10px 15px; border-radius: 8px; width: 350px; font-family: monospace; font-size: 0.85em; transition: all 0.2s; }
    .token-input:focus { border-color: #4a90e2; outline: none; box-shadow: 0 0 10px rgba(74, 144, 226, 0.2); }
    .btn-save { background: #4a90e2; color: #fff; border: none; padding: 10px 20px; border-radius: 8px; cursor: pointer; font-weight: bold; transition: background 0.2s; }
    .btn-save:hover { background: #357abd; }
    
    .status-indicator { display: flex; align-items: center; gap: 8px; }
    .status-dot { width: 10px; height: 10px; border-radius: 50%; background: #e74c3c; box-shadow: 0 0 8px #e74c3c; }
    .status-dot.valid { background: #2ecc71; box-shadow: 0 0 8px #2ecc71; }
    .status-text { font-size: 0.8em; color: #888; text-transform: uppercase; font-weight: bold; }

    .content-area { padding-top: 0; }
    
    .app-footer { padding: 20px; text-align: center; color: #444; font-size: 0.8em; border-top: 1px solid rgba(255,255,255,0.05); }
  `]
})
export class AppComponent {
  token: string = '';
  hasToken: boolean = false;

  constructor(private authService: AuthService) {
    this.token = this.authService.getToken() || '';
    this.authService.token$.subscribe(t => {
      this.hasToken = !!t;
    });
  }

  saveToken(): void {
    const raw = this.token.trim();
    const clean = raw.startsWith('Bearer ') ? raw.substring(7) : raw;
    this.authService.setToken(clean);
  }
}
