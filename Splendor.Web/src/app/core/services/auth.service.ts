import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private readonly TOKEN_KEY = 'splendor_auth_token';
    private tokenSubject = new BehaviorSubject<string | null>(this.getToken());

    constructor() { }

    setToken(token: string): void {
        localStorage.setItem(this.TOKEN_KEY, token);
        this.tokenSubject.next(token);
    }

    getToken(): string | null {
        return localStorage.getItem(this.TOKEN_KEY);
    }

    get token$(): Observable<string | null> {
        return this.tokenSubject.asObservable();
    }

    logout(): void {
        localStorage.removeItem(this.TOKEN_KEY);
        this.tokenSubject.next(null);
    }
}
