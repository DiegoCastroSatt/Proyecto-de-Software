import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { PortalClienteComponent } from './portal-cliente';
import { AutenticacionClienteService, ClienteSesion } from '../../shared/services/autenticacion-cliente.service';

describe('Portal del cliente', () => {
  const cliente = signal<ClienteSesion | null>(null);
  beforeEach(() => {
    cliente.set({ idCliente: 1, nombre: 'Ana Pérez', correo: 'ana@example.test' });
    TestBed.configureTestingModule({
      imports: [PortalClienteComponent],
      providers: [provideRouter([]), { provide: AutenticacionClienteService, useValue: { cliente } }]
    });
  });

  it('personaliza el portal con la identidad real de la sesión', () => {
    const fixture = TestBed.createComponent(PortalClienteComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('Portal del cliente');
    expect(fixture.nativeElement.querySelector('#bienvenida-titulo').textContent).toContain('Hola, Ana.');
    expect(fixture.nativeElement.querySelector('.account-name').textContent).toBe('Ana Pérez');
    expect(fixture.nativeElement.querySelector('.account-email').textContent).toBe('ana@example.test');
    expect(fixture.nativeElement.querySelector('.avatar').textContent).toBe('AP');
  });

  it('ofrece accesos a funciones existentes sin mostrar datos administrativos', () => {
    const fixture = TestBed.createComponent(PortalClienteComponent);
    fixture.detectChanges();
    const enlaces = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(enlaces.map(enlace => enlace.getAttribute('href')))
      .toEqual(['/reservas', '/mis-recetas', '/recomendador-marcos', '/probador-marcos', '/productos']);
  });
});
