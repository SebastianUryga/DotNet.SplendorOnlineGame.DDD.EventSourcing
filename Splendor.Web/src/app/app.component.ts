import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from './core/services/auth.service';

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
