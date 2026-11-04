import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AutenticacionClienteService } from '../shared/services/autenticacion-cliente.service';

export const authClienteGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);
  const servicio = inject(AutenticacionClienteService);
  const login = () => router.createUrlTree(['/cliente/login'], { queryParams: { returnUrl: state.url } });

  // En SSR no está disponible la cookie HttpOnly del navegador para consultar la sesión.
  // Dejar pasar la ruta permite que el guard del navegador la valide tras hidratarse.
  if (!isPlatformBrowser(inject(PLATFORM_ID))) return true;

  return servicio.verificarSesion().pipe(
    map(cliente => Number.isInteger(cliente.idCliente) && cliente.idCliente > 0 ? true : login()),
    catchError(() => {
      servicio.cliente.set(null);
      return of(login());
    })
  );
};
