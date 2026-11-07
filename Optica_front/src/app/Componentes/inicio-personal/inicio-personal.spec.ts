import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideHttpClient } from '@angular/common/http';
import { InicioPersonalComponent, SinAccesoComponent } from './inicio-personal';
import { AutenticacionAdminService } from '../../shared/services/autenticacion-admin.service';

describe('Inicio del personal', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [
    provideHttpClient(),
    provideRouter([{ path: 'personal/panel', children: [
      { path: '', component: InicioPersonalComponent },
      { path: 'agenda-configurada', component: SinAccesoComponent, data: { permiso: 'agenda', tituloInicio: 'Mi agenda' } },
      { path: 'ventas', component: SinAccesoComponent, data: { permiso: 'ventas', tituloInicio: 'Mis ventas' } },
      { path: 'detalle/:id', component: SinAccesoComponent, data: { permiso: 'agenda' } },
    ] }]),
  ] }));

  it('obtiene nombre y destino de las rutas configuradas y respeta los permisos', async () => {
    const sesion = TestBed.inject(AutenticacionAdminService);
    sesion.administrador.set({ idAdministrador: 0, idTrabajador: 1, nombre: 'Ana', tipoUsuario: 'Trabajador', rol: 'Atención', permisos: ['agenda'] });
    const harness = await RouterTestingHarness.create('/personal/panel');
    const enlaces = harness.routeNativeElement!.querySelectorAll('.opciones a');
    expect(enlaces.length).toBe(1);
    expect(enlaces[0].textContent).toBe('Mi agenda');
    expect(enlaces[0].getAttribute('href')).toBe('/personal/panel/agenda-configurada');
    expect(harness.routeNativeElement!.textContent).toContain('Hola, Ana');
  });

  it('actualiza los accesos al cambiar los permisos de la sesión', async () => {
    const sesion = TestBed.inject(AutenticacionAdminService);
    const trabajador = { idAdministrador: 0, idTrabajador: 1, nombre: 'Ana', tipoUsuario: 'Trabajador' as const, rol: 'Atención', permisos: ['agenda'] };
    sesion.administrador.set(trabajador);
    const harness = await RouterTestingHarness.create('/personal/panel');
    sesion.administrador.set({ ...trabajador, permisos: ['ventas'] });
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('.opciones')!.textContent).toContain('Mis ventas');
    expect(harness.routeNativeElement!.querySelector('.opciones')!.textContent).not.toContain('Mi agenda');
  });
});
