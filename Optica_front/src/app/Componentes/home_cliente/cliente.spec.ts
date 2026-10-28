import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { PLATFORM_ID } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { ClienteLayoutComponent } from './cliente';
import { AutenticacionClienteService } from '../login-cliente/autenticacion-cliente.service';

const cliente = { idCliente: 1, nombre: 'Ana Pérez', correo: 'ana@example.test' };
const urlSesion = 'http://localhost:8080/api/Autenticacion/cliente/sesion';

describe('ClienteLayoutComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ClienteLayoutComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('recupera la sesión al recargar y conserva los enlaces públicos con el saludo', () => {
    const fixture = TestBed.createComponent(ClienteLayoutComponent);
    fixture.detectChanges();
    const peticion = TestBed.inject(HttpTestingController).expectOne(urlSesion);
    expect(peticion.request.withCredentials).toBe(true);
    peticion.flush(cliente);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Bienvenido, Ana Pérez');
    expect(fixture.nativeElement.querySelector('.login-button')).toBeNull();
    expect(fixture.nativeElement.querySelectorAll('.client-tools-options a').length).toBe(2);
    expect(fixture.nativeElement.querySelector('.client-tools').open).toBe(false);
    expect(fixture.nativeElement.querySelector('a[href="/productos"]')).toBeTruthy();
  });

  it('muestra iniciar sesión cuando el visitante no está autenticado', () => {
    const fixture = TestBed.createComponent(ClienteLayoutComponent);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController).expectOne(urlSesion).flush({}, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.login-button').textContent).toContain('Iniciar sesión');
    expect(fixture.nativeElement.querySelector('.client-welcome')).toBeNull();
    expect(fixture.nativeElement.querySelector('.client-tools')).toBeNull();
    expect(fixture.nativeElement.querySelector('nav > a[href="/probador-marcos"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('nav > a[href="/recomendador-marcos"]')).toBeTruthy();
  });

  it('usa la sesión del login sin volver a consultarla', () => {
    TestBed.inject(AutenticacionClienteService).cliente.set(cliente);
    const fixture = TestBed.createComponent(ClienteLayoutComponent);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController).expectNone(urlSesion);
    expect(fixture.nativeElement.textContent).toContain('Bienvenido, Ana Pérez');
  });


  it('actualiza el nombre después del login y permite cerrar sesión desde el menú', () => {
    const fixture = TestBed.createComponent(ClienteLayoutComponent);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(urlSesion).flush({}, { status: 401, statusText: 'Unauthorized' });
    const servicio = TestBed.inject(AutenticacionClienteService);
    servicio.iniciarSesion('ana@example.test', 'Clave123!').subscribe();
    http.expectOne('http://localhost:8080/api/Autenticacion/cliente/login').flush(cliente);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.client-account summary').textContent).toContain('Bienvenido, Ana Pérez');
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture.nativeElement.querySelector('.client-account').open = true;
    fixture.nativeElement.querySelector('.client-account-options button').click();
    const salida = http.expectOne('http://localhost:8080/api/Autenticacion/cliente/salir');
    expect(salida.request.method).toBe('POST');
    expect(salida.request.withCredentials).toBe(true);
    salida.flush(null, { status: 204, statusText: 'No Content' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.client-account')).toBeNull();
    expect(fixture.nativeElement.querySelector('.login-button')).toBeTruthy();
    expect(navegar).toHaveBeenCalledWith('/inicio');
  });

  it('conserva el nombre y permite reintentar si falla el cierre de sesión', () => {
    TestBed.inject(AutenticacionClienteService).cliente.set(cliente);
    const fixture = TestBed.createComponent(ClienteLayoutComponent);
    fixture.detectChanges();
    fixture.componentInstance.cerrarSesion();
    TestBed.inject(HttpTestingController).expectOne('http://localhost:8080/api/Autenticacion/cliente/salir')
      .flush({}, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.client-account summary').textContent).toContain('Ana Pérez');
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('No se pudo cerrar sesión');
    expect(fixture.nativeElement.querySelector('button').disabled).toBe(false);
  });

  it('no consulta una sesión privada durante el renderizado del servidor', () => {
    TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
    const fixture = TestBed.createComponent(ClienteLayoutComponent);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController).expectNone(urlSesion);
  });
});
