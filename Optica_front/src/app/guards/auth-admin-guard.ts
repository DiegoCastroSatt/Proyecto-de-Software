import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AutenticacionAdminService } from '../Componentes/login-admin/autenticacion-admin.service';

export const authAdminGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const servicio = inject(AutenticacionAdminService);
  const login = () => router.createUrlTree(['/admin'], { queryParams: { returnUrl: state.url } });
  if (!isPlatformBrowser(inject(PLATFORM_ID))) return login();
  return servicio.verificarSesion().pipe(
    map(() => {
      if (!servicio.esPersonal()) return login();
      const datos = route.data ?? {};
      if (
        (datos['soloAdministrador'] && !servicio.esAdministrador()) ||
        (datos['soloTrabajador'] && servicio.administrador()?.tipoUsuario !== 'Trabajador') ||
        (datos['permiso'] && !servicio.puede(datos['permiso']))
      )
        return router.createUrlTree(['/admin/panel/sin-acceso']);
      return true;
    }),
    catchError(() => {
      servicio.administrador.set(null);
      return of(login());
    }),
  );
};
