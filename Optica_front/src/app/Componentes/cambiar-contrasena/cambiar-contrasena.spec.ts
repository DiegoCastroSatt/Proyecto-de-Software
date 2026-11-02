import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { CambiarContrasenaComponent } from './cambiar-contrasena';
import { AutenticacionAdminService } from '../../shared/services/autenticacion-admin.service';
describe('Cambio de contraseña propio', () => {
  function crear() {
    const sesion = {
      administrador: signal<unknown>({ nombre: 'Equipo' }),
      cambiarContrasena: vi.fn(() => of({ mensaje: 'Actualizada' })),
    };
    TestBed.configureTestingModule({
      imports: [CambiarContrasenaComponent],
      providers: [provideRouter([]), { provide: AutenticacionAdminService, useValue: sesion }],
    });
    const fixture = TestBed.createComponent(CambiarContrasenaComponent);
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    return { fixture, componente: fixture.componentInstance, sesion, navegar };
  }
  afterEach(() => TestBed.resetTestingModule());
  it('rechaza campos vacíos, claves cortas o confirmaciones diferentes', () => {
    const { componente, sesion } = crear();
    componente.guardar();
    componente.formulario.setValue({
      contrasenaActual: 'anterior-prueba-123',
      nuevaContrasena: 'corta',
      confirmarContrasena: 'corta',
    });
    componente.guardar();
    componente.formulario.patchValue({
      nuevaContrasena: 'nueva-prueba-123',
      confirmarContrasena: 'diferente-prueba-123',
    });
    componente.guardar();
    expect(sesion.cambiarContrasena).not.toHaveBeenCalled();
  });
  it('envía solo las claves propias y pide iniciar sesión de nuevo', () => {
    const { componente, sesion, navegar } = crear();
    const claves = {
      contrasenaActual: 'anterior-prueba-123',
      nuevaContrasena: 'nueva-prueba-123',
      confirmarContrasena: 'nueva-prueba-123',
    };
    componente.formulario.setValue(claves);
    componente.guardar();
    expect(sesion.cambiarContrasena).toHaveBeenCalledWith(claves);
    expect(sesion.administrador()).toBeNull();
    expect(componente.formulario.controls.nuevaContrasena.value).toBe('');
    expect(navegar).toHaveBeenCalledWith(['/admin'], { queryParams: { claveActualizada: '1' } });
  });
  it('muestra el rechazo y permite volver a ingresar la clave actual', () => {
    const { componente, sesion, navegar } = crear();
    sesion.cambiarContrasena.mockReturnValue(
      throwError(() => ({ error: { mensaje: 'La contraseña actual es incorrecta.' } })),
    );
    componente.formulario.setValue({
      contrasenaActual: 'anterior-prueba-123',
      nuevaContrasena: 'nueva-prueba-123',
      confirmarContrasena: 'nueva-prueba-123',
    });
    componente.guardar();
    expect(componente.error()).toContain('actual es incorrecta');
    expect(componente.guardando()).toBe(false);
    expect(componente.formulario.controls.contrasenaActual.value).toBe('');
    expect(navegar).not.toHaveBeenCalled();
  });
});
