import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AutenticacionAdminService } from '../Componentes/login-admin/autenticacion-admin.service';

export const authAdminGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);
  const servicio = inject(AutenticacionAdminService);
  const login = () => router.createUrlTree(['/admin'], { queryParams: { returnUrl: state.url } });
  if (!isPlatformBrowser(inject(PLATFORM_ID))) return login();
  return servicio.verificarSesion().pipe(
    map(admin => Number.isInteger(admin.idAdministrador) && admin.idAdministrador > 0 ? true : login()),
    catchError(() => { servicio.administrador.set(null); return of(login()); })
  );
};
