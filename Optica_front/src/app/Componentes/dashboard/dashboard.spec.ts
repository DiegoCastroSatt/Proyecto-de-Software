import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { DashboardComponent } from './dashboard';

describe('DashboardComponent', () => {
  const url = 'http://localhost:8080/api/Dashboard/summary';
  beforeEach(() => TestBed.configureTestingModule({
    imports: [DashboardComponent],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
  }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('muestra la respuesta y termina la carga sin otra interacción', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    await fixture.whenStable();
    const boton = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(boton.disabled).toBe(true);
    TestBed.inject(HttpTestingController).expectOne(url).flush({
      customerCount: 8, lowStockProductCount: 2, pendingOrderCount: 5,
      todaySales: 25000, updatedAt: '2026-09-28T12:00:00Z'
    });
    await fixture.whenStable();
    const valores = Array.from(fixture.nativeElement.querySelectorAll('.metric-card strong') as NodeListOf<HTMLElement>).map(el => el.textContent?.trim());
    expect(valores.slice(0, 3)).toEqual(['8', '2', '5']);
    expect(valores[3]).toContain('25');
    expect(boton.disabled).toBe(false);
    expect(boton.textContent).toContain('Actualizar datos');
  });

  it('muestra el error y permite reintentar', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    await fixture.whenStable();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(url).flush({}, { status: 500, statusText: 'Error' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeTruthy();
    const boton = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(boton.disabled).toBe(false);
    boton.click();
    http.expectOne(url).flush({ customerCount: 9, lowStockProductCount: 0,
      pendingOrderCount: 1, todaySales: 0, updatedAt: '2026-09-28T12:00:00Z' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('.metric-card strong').textContent).toContain('9');
  });
});
