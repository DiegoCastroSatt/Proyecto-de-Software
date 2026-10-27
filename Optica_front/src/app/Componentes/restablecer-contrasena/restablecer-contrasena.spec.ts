import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { RestablecerContrasenaComponent } from './restablecer-contrasena';
import { AutenticacionClienteService } from '../login-cliente/autenticacion-cliente.service';

describe('RestablecerContrasenaComponent', () => {
  let fixture: ComponentFixture<RestablecerContrasenaComponent>;
  let token: string | null;
  const router = { navigateByUrl: vi.fn() };
  const autenticacion = { confirmarRecuperacion: vi.fn() };

  beforeEach(async () => {
    vi.resetAllMocks();
    token = 'token-recuperacion';
    autenticacion.confirmarRecuperacion.mockReturnValue(of({ mensaje: 'Contraseña actualizada.' }));
    await TestBed.configureTestingModule({
      imports: [RestablecerContrasenaComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => token } } } },
        { provide: Router, useValue: router },
        { provide: AutenticacionClienteService, useValue: autenticacion }
      ]
    }).compileComponents();
  });

  function crearComponente(): void {
    fixture = TestBed.createComponent(RestablecerContrasenaComponent);
    fixture.detectChanges();
  }

  it('marca como inválido el enlace cuando no tiene token', () => {
    token = null;
    crearComponente();

    expect(fixture.componentInstance.tokenInvalido()).toBe(true);
    expect(autenticacion.confirmarRecuperacion).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Enlace no disponible');
  });

  it('envía las contraseñas cuando cumplen las reglas y son iguales', () => {
    crearComponente();
    fixture.componentInstance.nuevaContrasena = 'NuevaClave123!';
    fixture.componentInstance.confirmarContrasena = 'NuevaClave123!';

    fixture.componentInstance.restablecer();
    fixture.detectChanges();

    expect(autenticacion.confirmarRecuperacion).toHaveBeenCalledWith(
      'token-recuperacion', 'NuevaClave123!', 'NuevaClave123!'
    );
    expect(fixture.componentInstance.exito()).toBe(true);
  });

  it.each([
    ['', '', 'Completa ambos campos'],
    ['sololetras', 'sololetras', 'al menos 8 caracteres'],
    ['NuevaClave123!', 'OtraClave123!', 'no coinciden']
  ])('rechaza contraseñas inválidas antes de llamar a la API', (nueva, confirmar, mensaje) => {
    crearComponente();
    fixture.componentInstance.nuevaContrasena = nueva;
    fixture.componentInstance.confirmarContrasena = confirmar;

    fixture.componentInstance.restablecer();
    fixture.detectChanges();

    expect(autenticacion.confirmarRecuperacion).not.toHaveBeenCalled();
    expect(fixture.componentInstance.error()).toContain(mensaje);
  });

  it('muestra el error de la API si el enlace ya no sirve', () => {
    autenticacion.confirmarRecuperacion.mockReturnValueOnce(throwError(() => ({ error: { mensaje: 'El token venció.' } })));
    crearComponente();
    fixture.componentInstance.nuevaContrasena = 'NuevaClave123!';
    fixture.componentInstance.confirmarContrasena = 'NuevaClave123!';

    fixture.componentInstance.restablecer();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBe('El token venció.');
    expect(fixture.componentInstance.cargando()).toBe(false);
  });

  it('navega al login al pedir volver', () => {
    crearComponente();
    fixture.componentInstance.irAlLogin();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/cliente/login');
  });
});
