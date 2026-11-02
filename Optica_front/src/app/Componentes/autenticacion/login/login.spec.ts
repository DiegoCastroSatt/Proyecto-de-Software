import { Router, provideRouter } from '@angular/router';
import { AutenticacionService } from '../../../shared/services/autenticacion.service';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { LoginComponent } from './login';
import { AutenticacionClienteService } from '../../../shared/services/autenticacion-cliente.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  const servicio = {
    iniciarSesion: vi.fn(),
    registrar: vi.fn(),
    solicitarRecuperacion: vi.fn(),
    confirmarRecuperacion: vi.fn(),
    reenviarConfirmacion: vi.fn(),
    verificarSesion: vi.fn(),
    cerrarSesion: vi.fn()
  };

  beforeEach(async () => {
    vi.resetAllMocks();
    servicio.iniciarSesion.mockReturnValue(of({ tipoUsuario: 'Cliente', cliente: { idCliente: 1, nombre: 'Ana Pérez', correo: 'ana@example.test' }, personal: null }));
    servicio.registrar.mockReturnValue(of({ mensaje: 'Cuenta creada.' }));
    servicio.solicitarRecuperacion.mockReturnValue(of({ mensaje: 'Solicitud aceptada.' }));
    servicio.confirmarRecuperacion.mockReturnValue(of({ mensaje: 'Contraseña actualizada.' }));
    servicio.reenviarConfirmacion.mockReturnValue(of({ mensaje: 'Si existe una cuenta pendiente, enviaremos un enlace.' }));
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideRouter([]), { provide: AutenticacionClienteService, useValue: servicio }, { provide: AutenticacionService, useValue: servicio }]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
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

  it('muestra y oculta las contraseñas del registro sin enviar el formulario', () => {
    fixture.nativeElement.querySelectorAll('.registro-invitacion')[1].querySelector('button').click();
    fixture.detectChanges();

    const contrasena = fixture.nativeElement.querySelector('#registro-contrasena') as HTMLInputElement;
    const mostrar = fixture.nativeElement.querySelector('[aria-controls="registro-contrasena"]') as HTMLButtonElement;
    expect(contrasena.type).toBe('password');
    mostrar.click();
    fixture.detectChanges();
    expect(contrasena.type).toBe('text');
    expect(mostrar.getAttribute('aria-pressed')).toBe('true');
    mostrar.click();
    fixture.detectChanges();
    expect(contrasena.type).toBe('password');
    expect(servicio.registrar).not.toHaveBeenCalled();
  });

  it('muestra y oculta la nueva contraseña de recuperación sin confirmar el cambio', () => {
    fixture.nativeElement.querySelector('.enlace-secundario').click();
    component.recuperacionEnviada.set(true);
    fixture.detectChanges();

    const contrasena = fixture.nativeElement.querySelector('#nueva-contrasena-recuperacion') as HTMLInputElement;
    const mostrar = fixture.nativeElement.querySelector('[aria-controls="nueva-contrasena-recuperacion"]') as HTMLButtonElement;
    expect(contrasena.type).toBe('password');
    mostrar.click();
    fixture.detectChanges();
    expect(contrasena.type).toBe('text');
    mostrar.click();
    fixture.detectChanges();
    expect(contrasena.type).toBe('password');
    expect(servicio.confirmarRecuperacion).not.toHaveBeenCalled();
  });

  it('muestra un mensaje seguro cuando las credenciales son incorrectas', () => {
    servicio.iniciarSesion.mockReturnValueOnce(throwError(() => ({ status: 401 })));
    component.correo = 'ana@example.test';
    component.contrasena = 'incorrecta';

    component.iniciarSesion();
    fixture.detectChanges();

    expect(component.errorLogin()).toContain('Si no tienes cuenta');
  });

  it('envía una solicitud de recuperación por RUT y permite confirmar el código', () => {
    component.correoRecuperacion = '12.345.678-5';
    component.enviarRecuperacion();

    expect(servicio.solicitarRecuperacion).toHaveBeenCalledWith('12.345.678-5');
    expect(component.recuperacionEnviada()).toBe(true);
    component.codigoRecuperacion = '123456';
    component.nuevaContrasenaRecuperacion = 'ClaveNueva123!';
    component.confirmarContrasenaRecuperacion = 'ClaveNueva123!';
    component.confirmarCodigoRecuperacion();
    expect(servicio.confirmarRecuperacion).toHaveBeenCalledWith(expect.objectContaining({
      identificador: '12.345.678-5', codigo: '123456'
    }));
  });

  it('conecta el botón Enviar código con la solicitud HTTP', async () => {
    fixture.nativeElement.querySelector('.enlace-secundario').click();
    fixture.detectChanges();

    component.correoRecuperacion = '12.345.678-5';
    const formulario = fixture.nativeElement.querySelector('#recuperar-correo')!.closest('form') as HTMLFormElement;
    expect(formulario.querySelector('button[type="submit"]')?.textContent).toContain('Enviar código');
    (formulario.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(servicio.solicitarRecuperacion).toHaveBeenCalledWith('12.345.678-5');
    expect(servicio.solicitarRecuperacion).toHaveBeenCalledTimes(1);
    expect(fixture.nativeElement.textContent).toContain('Revisa tu correo');
  });

});
