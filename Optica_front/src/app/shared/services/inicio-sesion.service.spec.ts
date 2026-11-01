import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { InicioSesionService } from './inicio-sesion.service';
import { AutenticacionAdminService } from '../../Componentes/login-admin/autenticacion-admin.service';
import { AutenticacionClienteService } from '../../Componentes/login-cliente/autenticacion-cliente.service';

describe('Inicio de sesión común', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it.each(['Administrador', 'Trabajador'] as const)('actualiza el personal %s y retira la identidad de cliente anterior', tipoUsuario => {
    const clientes = TestBed.inject(AutenticacionClienteService);
    clientes.cliente.set({ idCliente: 5, nombre: 'Anterior', correo: 'anterior@example.test' });
    TestBed.inject(InicioSesionService).iniciarSesion('equipo', 'clave').subscribe();
    const req = TestBed.inject(HttpTestingController).expectOne('http://localhost:8080/api/Autenticacion/login');
    expect(req.request.withCredentials).toBe(true);
    expect(req.request.body).toEqual({ correo: 'equipo', contrasena: 'clave' });
    const personal = { tipoUsuario, idAdministrador: tipoUsuario === 'Administrador' ? 1 : 0, idTrabajador: 2, nombre: 'Equipo' };
    req.flush({ tipoUsuario, personal, cliente: null });
    expect(TestBed.inject(AutenticacionAdminService).administrador()).toEqual(personal);
    expect(clientes.cliente()).toBeNull();
  });

  it('actualiza el cliente y retira la identidad de administrador anterior', () => {
    const personal = TestBed.inject(AutenticacionAdminService);
    personal.administrador.set({ idAdministrador: 1, nombre: 'Anterior' });
    TestBed.inject(InicioSesionService).iniciarSesion('ana@example.test', 'clave').subscribe();
    const cliente = { idCliente: 5, nombre: 'Ana', correo: 'ana@example.test' };
    TestBed.inject(HttpTestingController).expectOne('http://localhost:8080/api/Autenticacion/login')
      .flush({ tipoUsuario: 'Cliente', personal: null, cliente });
    expect(personal.administrador()).toBeNull();
    expect(TestBed.inject(AutenticacionClienteService).cliente()).toEqual(cliente);
  });
});
