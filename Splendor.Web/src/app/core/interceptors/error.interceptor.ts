import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const toast = inject(ToastService);
    return next(req).pipe(
        catchError(err => {
            const e = err.error;
            const text = e?.error ?? (e?.errors ? Object.values(e.errors).flat()[0] : e?.title);
            if (err.status === 400 && text && !req.url.endsWith('/auth/guest')) {
                toast.show(String(text));
            }
            return throwError(() => err);
        })
    );
};
