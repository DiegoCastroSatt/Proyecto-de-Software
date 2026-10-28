import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AutenticacionAdminService } from '../Componentes/login-admin/autenticacion-admin.service';
import { obtenerOrigenApi } from '../shared/services/api-origin';

export const sesionAdminInterceptor: HttpInterceptorFn = (req, next) => {
  const origenLocal = 'http://localhost:8080';
  const origenApi = obtenerOrigenApi();
  if (!req.url.startsWith(`${origenLocal}/api/`)) return next(req);
  const router = inject(Router);
  const sesion = inject(AutenticacionAdminService);
  const url = `${origenApi}${req.url.slice(origenLocal.length)}`;
  return next(req.clone({ url, withCredentials: true })).pipe(catchError((error: HttpErrorResponse) => {
    if (error.status === 401 && router.url.startsWith('/admin/panel')) {
      sesion.administrador.set(null);
      void router.navigate(['/admin'], { queryParams: { returnUrl: router.url } });
    }
    return throwError(() => error);
  }));
};
