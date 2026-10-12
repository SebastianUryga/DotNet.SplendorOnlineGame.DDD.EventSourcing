import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const toast = inject(ToastService);
    const authService = inject(AuthService);
    return next(req).pipe(
        catchError(err => {
            if (err.status === 400 && err.error?.error && !req.url.endsWith('/auth/guest')) {
                toast.show(err.error.error);
            }
            if (err.status === 401 && !req.url.endsWith('/auth/guest')) {
                authService.requireLogin();
            }
            return throwError(() => err);
        })
    );
};
