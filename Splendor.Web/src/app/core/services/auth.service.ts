import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private readonly TOKEN_KEY = 'splendor_auth_token';
    private tokenSubject = new BehaviorSubject<string | null>(this.getToken());
    private loginRequiredSubject = new BehaviorSubject<boolean>(false);

    constructor() { }

    setToken(token: string): void {
        localStorage.setItem(this.TOKEN_KEY, token);
        this.tokenSubject.next(token);
        this.loginRequiredSubject.next(false);
    }

    getToken(): string | null {
        return localStorage.getItem(this.TOKEN_KEY);
    }

    get token$(): Observable<string | null> {
        return this.tokenSubject.asObservable();
    }

    get loginRequired$(): Observable<boolean> {
        return this.loginRequiredSubject.asObservable();
    }

    logout(): void {
        localStorage.removeItem(this.TOKEN_KEY);
        this.tokenSubject.next(null);
    }

    /** Called when the API rejects the token (401): drops it and asks the user to sign in again. */
    requireLogin(): void {
        this.logout();
        this.loginRequiredSubject.next(true);
    }

    dismissLogin(): void {
        this.loginRequiredSubject.next(false);
    }
}
