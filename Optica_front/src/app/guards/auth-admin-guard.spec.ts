import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom, Observable } from 'rxjs';
import { authAdminGuard } from './auth-admin-guard';

describe('Sesión administrativa', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    }),
  );
  afterEach(() => {
    sessionStorage.clear();
    TestBed.inject(HttpTestingController).verify();
  });
  const ejecutar = () =>
    TestBed.runInInjectionContext(() =>
      authAdminGuard({} as never, { url: '/admin/panel/ventas' } as RouterStateSnapshot),
    );
  it('consulta la identidad del servidor cada vez que se accede', async () => {
    for (let i = 0; i < 2; i++) {
      const resultado = firstValueFrom(ejecutar() as Observable<boolean | UrlTree>);
      TestBed.inject(HttpTestingController)
        .expectOne('http://localhost:8080/api/Autenticacion/admin/sesion')
        .flush({ idAdministrador: 3, nombre: 'Administrador' });
      expect(await resultado).toBe(true);
    }
  });
  it('rechaza la bandera local cuando la sesión no es válida', async () => {
    sessionStorage.setItem('isAdmin', 'true');
    const resultado = firstValueFrom(ejecutar() as Observable<boolean | UrlTree>);
    TestBed.inject(HttpTestingController)
      .expectOne('http://localhost:8080/api/Autenticacion/admin/sesion')
      .flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(TestBed.inject(Router).serializeUrl((await resultado) as UrlTree)).toContain(
      '/admin?returnUrl=',
    );
  });
  it('no permite acceso administrativo durante el renderizado del servidor', () => {
    TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
    expect(ejecutar() instanceof UrlTree).toBe(true);
  });
  it('bloquea un trabajador que intenta administrar trabajadores', async () => {
    const resultado = firstValueFrom(
      TestBed.runInInjectionContext(() =>
        authAdminGuard(
          { data: { soloAdministrador: true } } as never,
          { url: '/admin/panel/trabajadores' } as RouterStateSnapshot,
        ),
      ) as Observable<boolean | UrlTree>,
    );
    TestBed.inject(HttpTestingController)
      .expectOne('http://localhost:8080/api/Autenticacion/admin/sesion')
      .flush({
        idAdministrador: 0,
        nombre: 'Equipo',
        tipoUsuario: 'Trabajador',
        idTrabajador: 1,
        permisos: ['ventas'],
      });
    expect(TestBed.inject(Router).serializeUrl((await resultado) as UrlTree)).toBe(
      '/admin/panel/sin-acceso',
    );
  });
  it('permite solo las rutas del rol del trabajador', async () => {
    for (const permiso of ['ventas', 'clientes']) {
      const resultado = firstValueFrom(
        TestBed.runInInjectionContext(() =>
          authAdminGuard(
            { data: { permiso } } as never,
            { url: '/admin/panel/' + permiso } as RouterStateSnapshot,
          ),
        ) as Observable<boolean | UrlTree>,
      );
      TestBed.inject(HttpTestingController)
        .expectOne('http://localhost:8080/api/Autenticacion/admin/sesion')
        .flush({
          idAdministrador: 0,
          nombre: 'Equipo',
          tipoUsuario: 'Trabajador',
          idTrabajador: 1,
          permisos: ['ventas'],
        });
      expect(await resultado).toEqual(
        permiso === 'ventas'
          ? true
          : TestBed.inject(Router).createUrlTree(['/admin/panel/sin-acceso']),
      );
    }
  });
  it('rechaza una identidad de cliente aunque declare permisos internos', async () => {
    const resultado = firstValueFrom(ejecutar() as Observable<boolean | UrlTree>);
    TestBed.inject(HttpTestingController)
      .expectOne('http://localhost:8080/api/Autenticacion/admin/sesion')
      .flush({
        idAdministrador: 0,
        nombre: 'Cliente',
        tipoUsuario: 'Cliente',
        permisos: ['ventas'],
      });
    expect(TestBed.inject(Router).serializeUrl((await resultado) as UrlTree)).toContain(
      '/admin?returnUrl=',
    );
  });
});
