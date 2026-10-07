import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const toast = inject(ToastService);
    return next(req).pipe(
        catchError(err => {
            if (err.status === 400 && err.error?.error && !req.url.endsWith('/auth/guest')) {
                toast.show(err.error.error);
            }
            return throwError(() => err);
        })
    );
};
