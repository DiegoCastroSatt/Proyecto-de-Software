import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AutenticacionAdminService } from '../../shared/services/autenticacion-admin.service';
import { DashboardComponent } from '../dashboard/dashboard';
@Component({
  selector: 'app-inicio-personal',
  imports: [RouterLink, DashboardComponent],
  templateUrl: './inicio-personal.html',
  styleUrl: './inicio-personal.css',
})
export class InicioPersonalComponent {
  readonly sesion = inject(AutenticacionAdminService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly opciones = computed(() =>
    (this.route.parent?.routeConfig?.children ?? [])
      .filter(ruta => ruta.path && !ruta.redirectTo
        && typeof ruta.data?.['tituloInicio'] === 'string'
        && typeof ruta.data?.['permiso'] === 'string'
        && this.sesion.puede(ruta.data['permiso']))
      .map(ruta => ({
        permiso: ruta.data!['permiso'] as string,
        nombre: ruta.data!['tituloInicio'] as string,
        ruta: this.router.createUrlTree([ruta.path!], { relativeTo: this.route.parent }),
      })),
  );
}
@Component({
  selector: 'app-sin-acceso',
  imports: [RouterLink],
  styleUrl: './sin-acceso.css',
  templateUrl: './sin-acceso.html',
})
export class SinAccesoComponent {}
