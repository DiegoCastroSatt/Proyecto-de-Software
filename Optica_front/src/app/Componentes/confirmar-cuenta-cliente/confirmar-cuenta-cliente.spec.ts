import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { ConfirmarCuentaClienteComponent } from './confirmar-cuenta-cliente';
import { AutenticacionClienteService } from '../login-cliente/autenticacion-cliente.service';

describe('ConfirmarCuentaClienteComponent', () => {
  let fixture: ComponentFixture<ConfirmarCuentaClienteComponent>;
  let token: string | null;
  const router = { navigateByUrl: vi.fn() };
  const autenticacion = { confirmarCuenta: vi.fn() };

  beforeEach(async () => {
    vi.resetAllMocks();
    token = 'token-seguro';
    autenticacion.confirmarCuenta.mockReturnValue(of({ mensaje: 'Cuenta activada.' }));
    await TestBed.configureTestingModule({
      imports: [ConfirmarCuentaClienteComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => token } } } },
        { provide: Router, useValue: router },
        { provide: AutenticacionClienteService, useValue: autenticacion }
      ]
    }).compileComponents();
  });

  function crearComponente(): void {
    fixture = TestBed.createComponent(ConfirmarCuentaClienteComponent);
    fixture.detectChanges();
  }

  it('confirma la cuenta usando el token de la URL', () => {
    token = ' token-seguro ';
    crearComponente();

    expect(autenticacion.confirmarCuenta).toHaveBeenCalledWith('token-seguro');
    expect(fixture.nativeElement.textContent).toContain('Cuenta activada');
    expect(fixture.nativeElement.textContent).toContain('Cuenta activada.');
  });

  it('no llama a la API si falta el token', () => {
    token = '   ';
    crearComponente();

    expect(autenticacion.confirmarCuenta).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('no contiene un token');
  });

  it('muestra el mensaje de error que responde la API', () => {
    autenticacion.confirmarCuenta.mockReturnValueOnce(throwError(() => ({ error: { mensaje: 'Enlace vencido.' } })));
    crearComponente();

    expect(fixture.nativeElement.textContent).toContain('Enlace vencido.');
    expect(fixture.nativeElement.textContent).toContain('Enlace no disponible');
  });

  it('permite volver al login', () => {
    crearComponente();
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/cliente/login');
  });
});
