import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from './core/services/auth.service';
import { environment } from '../environments/environment';
import { ToastService } from './core/services/toast.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  token: string = '';
  hasToken: boolean = false;
  guestError: string = '';

  constructor(public authService: AuthService, private http: HttpClient, public toastService: ToastService) {
    this.token = this.authService.getToken() || '';
    this.authService.token$.subscribe(t => {
      this.hasToken = !!t;
    });
  }

  /** reload: from the session-expired dialog, so the current page picks up the new token. */
  playAsGuest(reload = false): void {
    this.guestError = '';
    this.http.post<{ token: string; displayName: string }>(`${environment.apiUrl}/auth/guest`, {}).subscribe({
      next: r => {
        localStorage.setItem('splendor_guest_name', r.displayName);
        this.token = r.token;
        this.authService.setToken(r.token);
        if (reload) location.reload();
      },
      error: e => this.guestError = e.status === 429
        ? 'Guest limit reached, try again later.'
        : 'Could not start guest session.'
    });
  }

  saveToken(reload = false): void {
    const raw = this.token.trim();
    const clean = raw.startsWith('Bearer ') ? raw.substring(7) : raw;
    this.authService.setToken(clean);
    if (reload) location.reload();
  }
}
