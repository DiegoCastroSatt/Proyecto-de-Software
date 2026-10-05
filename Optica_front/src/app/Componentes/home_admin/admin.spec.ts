import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AdminLayoutComponent } from './admin';

describe('AdminLayoutComponent', () => {
  let component: AdminLayoutComponent;
  let fixture: ComponentFixture<AdminLayoutComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminLayoutComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    fixture = TestBed.createComponent(AdminLayoutComponent);
    component = fixture.componentInstance;
    TestBed.inject(HttpTestingController).expectOne('http://localhost:8080/api/Productos?termino=').flush([
      { id: 1, nombre: 'Armazón clásico', stock: 4, stockMinimo: 3 },
      { id: 2, nombre: 'Lentes solares', stock: 8, stockMinimo: 3 },
    ]);
    await fixture.whenStable();
  });

  afterEach(() => {
    fixture.destroy();
    TestBed.inject(HttpTestingController).verify();
    vi.useRealTimers();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('crea y permite borrar una notificación cuando el stock cruza el mínimo', async () => {
    await vi.advanceTimersByTimeAsync(15_000);
    TestBed.inject(HttpTestingController).expectOne('http://localhost:8080/api/Productos?termino=').flush([
      { id: 1, nombre: 'Armazón clásico', stock: 3, stockMinimo: 3 },
      { id: 2, nombre: 'Lentes solares', stock: 8, stockMinimo: 3 },
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const boton = fixture.nativeElement.querySelector('.boton-notificaciones') as HTMLButtonElement;
    expect(boton.getAttribute('aria-label')).toContain('1 alerta de bajo stock');
    boton.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.panel-notificaciones').textContent).toContain('Armazón clásico');
    expect(fixture.nativeElement.querySelector('.panel-notificaciones').textContent).toContain('Quedan 3; mínimo 3.');
    expect(fixture.nativeElement.querySelector('.panel-notificaciones').textContent).not.toContain('Lentes solares');

    (fixture.nativeElement.querySelector('.boton-eliminar-notificacion') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.contador-notificaciones')).toBeNull();
    expect(fixture.nativeElement.querySelector('.panel-notificaciones').textContent).toContain('No hay alertas');
  });

  it('reanuda la detección después de un error al consultar el inventario', async () => {
    const http = TestBed.inject(HttpTestingController);
    await vi.advanceTimersByTimeAsync(15_000);
    http.expectOne('http://localhost:8080/api/Productos?termino=').flush({}, { status: 500, statusText: 'Error' });
    await fixture.whenStable();

    await vi.advanceTimersByTimeAsync(15_000);
    http.expectOne('http://localhost:8080/api/Productos?termino=').flush([
      { id: 1, nombre: 'Armazón clásico', stock: 4, stockMinimo: 3 },
      { id: 2, nombre: 'Lentes solares', stock: 8, stockMinimo: 3 },
    ]);
    await fixture.whenStable();

    (fixture.nativeElement.querySelector('.boton-notificaciones') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('.panel-notificaciones').textContent).toContain('No hay alertas');
  });
});
