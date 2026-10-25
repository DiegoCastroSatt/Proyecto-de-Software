import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { LoginCliente } from './login-cliente';
import { AutenticacionClienteService } from './autenticacion-cliente.service';

describe('LoginCliente', () => {
  let component: LoginCliente;
  let fixture: ComponentFixture<LoginCliente>;
  const servicio = {
    iniciarSesion: vi.fn(),
    registrar: vi.fn(),
    verificarSesion: vi.fn(),
    cerrarSesion: vi.fn()
  };

  beforeEach(async () => {
    vi.resetAllMocks();
    servicio.iniciarSesion.mockReturnValue(of({ idCliente: 1, nombre: 'Ana Pérez', correo: 'ana@example.test' }));
    servicio.registrar.mockReturnValue(of({ mensaje: 'Cuenta creada.' }));
    await TestBed.configureTestingModule({
      imports: [LoginCliente],
      providers: [{ provide: AutenticacionClienteService, useValue: servicio }]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginCliente);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('abre y cierra el modal de registro sin enviar datos', () => {
    const botonRegistro = fixture.nativeElement.querySelector('.registro-enlace') as HTMLButtonElement;
    botonRegistro.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).toBeTruthy();
    (fixture.nativeElement.querySelector('.registro-cancelar') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).toBeNull();
    expect(servicio.registrar).not.toHaveBeenCalled();
  });

  it('no envía el registro si los campos obligatorios son inválidos', () => {
    fixture.nativeElement.querySelector('.registro-enlace').click();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.registro-crear') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(servicio.registrar).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.campo-error')?.textContent).toContain('obligatorio');
  });

  it('muestra un mensaje seguro cuando las credenciales son incorrectas', () => {
    servicio.iniciarSesion.mockReturnValueOnce(throwError(() => ({ status: 401 })));
    component.correo = 'ana@example.test';
    component.contrasena = 'incorrecta';

    component.iniciarSesion();
    fixture.detectChanges();

    expect(component.errorLogin()).toContain('Si no tienes cuenta');
  });
});
