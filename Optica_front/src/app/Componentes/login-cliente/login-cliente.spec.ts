import { Router, provideRouter } from '@angular/router';
import { InicioSesionService } from '../../shared/services/inicio-sesion.service';
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
    solicitarRecuperacion: vi.fn(),
    reenviarConfirmacion: vi.fn(),
    verificarSesion: vi.fn(),
    cerrarSesion: vi.fn()
  };

  beforeEach(async () => {
    vi.resetAllMocks();
    servicio.iniciarSesion.mockReturnValue(of({ tipoUsuario: 'Cliente', cliente: { idCliente: 1, nombre: 'Ana Pérez', correo: 'ana@example.test' }, personal: null }));
    servicio.registrar.mockReturnValue(of({ mensaje: 'Cuenta creada.' }));
    servicio.solicitarRecuperacion.mockReturnValue(of({ mensaje: 'Solicitud aceptada.' }));
    servicio.reenviarConfirmacion.mockReturnValue(of({ mensaje: 'Si existe una cuenta pendiente, enviaremos un enlace.' }));
    await TestBed.configureTestingModule({
      imports: [LoginCliente],
      providers: [provideRouter([]), { provide: AutenticacionClienteService, useValue: servicio }, { provide: InicioSesionService, useValue: servicio }]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginCliente);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it.each(['Administrador', 'Trabajador', 'Cliente'])('redirige según el tipo %s validado por el servidor', (tipoUsuario) => {
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    servicio.iniciarSesion.mockReturnValueOnce(of({ tipoUsuario }));
    component.correo = tipoUsuario === 'Administrador' ? 'admin' : tipoUsuario === 'Trabajador' ? 'equipo' : 'cuenta@example.test';
    component.contrasena = 'ClaveDePrueba123!';
    component.iniciarSesion();
    expect(navegar).toHaveBeenCalledWith(tipoUsuario === 'Cliente' ? '/cliente/inicio' : '/admin/panel');
    expect(component.contrasena).toBe('');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('abre y cierra el modal de registro sin enviar datos', () => {
    const botonRegistro = fixture.nativeElement.querySelectorAll('.registro-invitacion')[1]
      .querySelector('button') as HTMLButtonElement;
    botonRegistro.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).toBeTruthy();
    (fixture.nativeElement.querySelector('.registro-cancelar') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).toBeNull();
    expect(servicio.registrar).not.toHaveBeenCalled();
  });

  it('no envía el registro si los campos obligatorios son inválidos', () => {
    fixture.nativeElement.querySelectorAll('.registro-invitacion')[1].querySelector('button').click();
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

  it('envía una solicitud de recuperación al pulsar el botón del modal', () => {
    component.correo = 'ana@example.test';
    fixture.nativeElement.querySelector('.enlace-secundario').click();
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.registro-crear') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(servicio.solicitarRecuperacion).toHaveBeenCalledWith('ana@example.test');
    expect(fixture.nativeElement.textContent).toContain('Enlace enviado');
  });
});
