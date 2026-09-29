import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom, Observable } from 'rxjs';
import { authAdminGuard } from './auth-admin-guard';

describe('Sesión administrativa', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), { provide: PLATFORM_ID, useValue: 'browser' }] }));
  afterEach(() => { sessionStorage.clear(); TestBed.inject(HttpTestingController).verify(); });
  const ejecutar = () => TestBed.runInInjectionContext(() => authAdminGuard({} as never, { url: '/admin/panel/ventas' } as RouterStateSnapshot));
  it('consulta la identidad del servidor cada vez que se accede', async () => {
    for (let i = 0; i < 2; i++) {
      const resultado = firstValueFrom(ejecutar() as Observable<boolean | UrlTree>);
      TestBed.inject(HttpTestingController).expectOne('http://localhost:8080/api/Autenticacion/admin/sesion').flush({ idAdministrador: 3, nombre: 'Administrador' });
      expect(await resultado).toBe(true);
    }
  });
  it('rechaza la bandera local cuando la sesión no es válida', async () => {
    sessionStorage.setItem('isAdmin', 'true');
    const resultado = firstValueFrom(ejecutar() as Observable<boolean | UrlTree>);
    TestBed.inject(HttpTestingController).expectOne('http://localhost:8080/api/Autenticacion/admin/sesion').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(TestBed.inject(Router).serializeUrl(await resultado as UrlTree)).toContain('/admin?returnUrl=');
  });
  it('no permite acceso administrativo durante el renderizado del servidor', () => {
    TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
    expect(ejecutar() instanceof UrlTree).toBe(true);
  });
});
