import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { LoginAdmin } from './login-admin';
import { AutenticacionAdminService } from './autenticacion-admin.service';

describe('Acceso y recuperación del personal', () => {
  let component: LoginAdmin;
  let fixture: ComponentFixture<LoginAdmin>;
  let http: HttpTestingController;
  const url = 'http://localhost:8080/api/Autenticacion/admin';

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginAdmin],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
    fixture = TestBed.createComponent(LoginAdmin);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture.detectChanges();
  });
  afterEach(() => http.verify());

  it('inicia sesión del personal con usuario y guarda la identidad recibida', () => {
    component.usuario = ' trabajador.ventas ';
    component.contrasena = 'ClaveDePrueba123!';
    component.onLogin();
    const req = http.expectOne(url);
    expect(req.request.body.nombreUsuario).toBe('trabajador.ventas');
    req.flush({ idAdministrador: 0, idTrabajador: 4, nombre: 'Trabajador', tipoUsuario: 'Trabajador', permisos: ['ventas'] });
    expect(TestBed.inject(AutenticacionAdminService).administrador()?.idTrabajador).toBe(4);
    expect(TestBed.inject(Router).navigateByUrl).toHaveBeenCalledWith('/admin/panel');
  });

  it('solicita y confirma el código usando el mismo nombre de usuario', () => {
    component.usuario = 'equipo';
    component.abrirRecuperacion();
    component.solicitarCodigo();
    const solicitud = http.expectOne(`${url}/recuperacion/solicitar`);
    expect(solicitud.request.body).toEqual({ nombreUsuario: 'equipo' });
    solicitud.flush({ mensaje: 'Revisa el correo asociado.' });
    expect(component.mostrarConfirmacionRecuperacion()).toBe(true);
    component.codigoRecuperacion = '123456';
    component.nuevaContrasena = 'NuevaClavePrueba123!';
    component.confirmarContrasena = component.nuevaContrasena;
    component.confirmarRecuperacion();
    const confirmacion = http.expectOne(`${url}/recuperacion/confirmar`);
    expect(confirmacion.request.body.nombreUsuario).toBe('equipo');
    expect(confirmacion.request.body.codigo).toBe('123456');
    confirmacion.flush({ mensaje: 'Contraseña actualizada.' });
    expect(component.exito()).toBe('Contraseña actualizada.');
    expect(component.mostrarConfirmacionRecuperacion()).toBe(false);
    expect(component.nuevaContrasena).toBe('');
  });

  it('mantiene la confirmación abierta cuando el código ha expirado', () => {
    component.mostrarConfirmacionRecuperacion.set(true);
    component.nombreUsuarioRecuperacion = 'equipo';
    component.codigoRecuperacion = '123456';
    component.nuevaContrasena = component.confirmarContrasena = 'NuevaClavePrueba123!';
    component.confirmarRecuperacion();
    http.expectOne(`${url}/recuperacion/confirmar`).flush({ mensaje: 'Código expirado.' }, { status: 400, statusText: 'Bad Request' });
    expect(component.errorRecuperacion()).toBe('Código expirado.');
    expect(component.mostrarConfirmacionRecuperacion()).toBe(true);
    expect(component.procesandoRecuperacion()).toBe(false);
  });
});
