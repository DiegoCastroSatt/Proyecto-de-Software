import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AutenticacionAdminService } from '../Componentes/login-admin/autenticacion-admin.service';

export const sesionAdminInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('http://localhost:8080/api/')) return next(req);
  const router = inject(Router);
  const sesion = inject(AutenticacionAdminService);
  return next(req.clone({ withCredentials: true })).pipe(catchError((error: HttpErrorResponse) => {
    if (error.status === 401 && router.url.startsWith('/admin/panel')) {
      sesion.administrador.set(null);
      void router.navigate(['/admin'], { queryParams: { returnUrl: router.url } });
    }
    return throwError(() => error);
  }));
};
