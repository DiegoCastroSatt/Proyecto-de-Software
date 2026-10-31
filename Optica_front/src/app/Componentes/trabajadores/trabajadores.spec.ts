import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { TrabajadoresComponent } from './trabajadores';
import { AccesosService } from './accesos.service';

describe('Gestión de trabajadores y roles', () => {
  function crear() {
    const api = {
      permisos: vi.fn(() => of([{ codigo: 'ventas', nombre: 'Registrar ventas' }])),
      roles: vi.fn(() => of([{ idRol: 1, nombre: 'Ventas', permisos: ['ventas'] }])),
      trabajadores: vi.fn(() =>
        of([
          {
            idTrabajador: 1,
            nombre: 'Equipo',
            correo: 'equipo@example.com',
            estado: 'Activo' as const,
            idRol: 1,
            rol: 'Ventas',
            tipoUsuario: 'Trabajador' as const,
          },
        ]),
      ),
      crearTrabajador: vi.fn(() => of({})),
      guardarRol: vi.fn(() => of({})),
      asignarAcceso: vi.fn(() => of(undefined)),
    };
    TestBed.configureTestingModule({
      imports: [TrabajadoresComponent],
      providers: [{ provide: AccesosService, useValue: api }],
    });
    const fixture = TestBed.createComponent(TrabajadoresComponent);
    fixture.detectChanges();
    return { fixture, componente: fixture.componentInstance, api };
  }
  afterEach(() => TestBed.resetTestingModule());
  it('carga el equipo y muestra las opciones para agregar trabajadores', () => {
    const { fixture } = crear();
    expect(fixture.nativeElement.textContent).toContain('Agregar trabajador');
    expect(fixture.nativeElement.textContent).toContain('equipo@example.com');
  });
  it('valida los campos antes de crear y envía nombre, correo y rol', () => {
    const { componente, api } = crear();
    componente.crearTrabajador();
    expect(api.crearTrabajador).not.toHaveBeenCalled();
    componente.trabajadorForm.setValue({
      nombre: 'Persona de prueba',
      correo: 'nueva@example.com',
      contrasena: 'solo-para-pruebas-123',
      idRol: 1,
    });
    componente.crearTrabajador();
    expect(api.crearTrabajador).toHaveBeenCalledWith({
      nombre: 'Persona de prueba',
      correo: 'nueva@example.com',
      contrasena: 'solo-para-pruebas-123',
      idRol: 1,
    });
    expect(componente.trabajadorForm.controls.contrasena.value).toBe('');
    expect(componente.mensaje()).toContain('Trabajador creado');
  });
  it('guarda cambios de rol y estado del trabajador', () => {
    const { componente, api } = crear();
    const trabajador = { ...componente.trabajadores()[0], estado: 'Inactivo' as const };
    componente.guardarAcceso(trabajador);
    expect(api.asignarAcceso).toHaveBeenCalledWith(trabajador);
  });
  it('edita los permisos de un rol y permite dejarlo sin funciones', () => {
    const { componente, api } = crear();
    componente.editarRol(componente.roles()[0]);
    componente.alternarPermiso('ventas', false);
    componente.guardarRol();
    expect(api.guardarRol).toHaveBeenCalledWith(1, { nombre: 'Ventas', permisos: [] });
  });
  it('muestra el rechazo del servidor sin anunciar una creación exitosa', () => {
    const { componente, api } = crear();
    api.crearTrabajador.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 403 })));
    componente.trabajadorForm.setValue({
      nombre: 'Prueba',
      correo: 'prueba@example.com',
      contrasena: 'solo-para-pruebas-123',
      idRol: 1,
    });
    componente.crearTrabajador();
    expect(componente.error()).toContain('Solo un administrador');
    expect(componente.mensaje()).toBe('');
  });
  function prepararDialogo(fixture: ReturnType<typeof crear>['fixture']) {
    const dialogo = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    Object.defineProperty(dialogo, 'showModal', {
      value: vi.fn(() => dialogo.setAttribute('open', '')),
    });
    Object.defineProperty(dialogo, 'close', {
      value: vi.fn(() => dialogo.removeAttribute('open')),
    });
    return dialogo;
  }
  it('abre permisos desde Agregar trabajador sin borrar sus datos', () => {
    const { fixture, componente } = crear();
    const dialogo = prepararDialogo(fixture);
    componente.nuevo.set(true);
    componente.trabajadorForm.patchValue({ nombre: 'Persona nueva', correo: 'nueva@example.com' });
    componente.abrirRolEnTrabajador();
    fixture.detectChanges();
    expect(dialogo.hasAttribute('open')).toBe(true);
    expect(dialogo.textContent).toContain('Funciones permitidas');
    expect(dialogo.querySelector('input[type=checkbox]')).toBeTruthy();
    expect(componente.vista()).toBe('trabajadores');
    expect(componente.trabajadorForm.controls.nombre.value).toBe('Persona nueva');
    expect(componente.trabajadorForm.controls.correo.value).toBe('nueva@example.com');
  });
  it('crea el rol, lo selecciona y conserva el trabajador pendiente', () => {
    const { fixture, componente, api } = crear();
    const dialogo = prepararDialogo(fixture);
    componente.nuevo.set(true);
    componente.trabajadorForm.patchValue({
      nombre: 'Persona nueva',
      correo: 'nueva@example.com',
      contrasena: 'solo-para-pruebas-123',
    });
    componente.abrirRolEnTrabajador();
    componente.rolForm.controls.nombre.setValue('Encargado');
    componente.alternarPermiso('ventas', true);
    api.guardarRol.mockReturnValue(of({ idRol: 2, nombre: 'Encargado', permisos: ['ventas'] }));
    componente.guardarRol();
    fixture.detectChanges();
    expect(api.guardarRol).toHaveBeenCalledWith(null, {
      nombre: 'Encargado',
      permisos: ['ventas'],
    });
    expect(componente.trabajadorForm.controls.idRol.value).toBe(2);
    expect(componente.rolSeleccionado?.nombre).toBe('Encargado');
    expect(componente.trabajadorForm.controls.correo.value).toBe('nueva@example.com');
    expect(componente.trabajadorForm.controls.contrasena.value).toBe('solo-para-pruebas-123');
    expect(componente.nuevo()).toBe(true);
    expect(componente.vista()).toBe('trabajadores');
    expect(dialogo.hasAttribute('open')).toBe(false);
    expect(fixture.nativeElement.querySelector('.resumen-permisos').textContent).toContain(
      'Registrar y consultar ventas',
    );
    expect(api.crearTrabajador).not.toHaveBeenCalled();
  });
  it('permite cancelar la creación de rol sin cambiar el seleccionado', () => {
    const { fixture, componente, api } = crear();
    prepararDialogo(fixture);
    componente.trabajadorForm.controls.idRol.setValue(1);
    componente.abrirRolEnTrabajador();
    componente.cancelarRolEnTrabajador();
    expect(componente.trabajadorForm.controls.idRol.value).toBe(1);
    expect(componente.creandoRolEnTrabajador()).toBe(false);
    expect(api.guardarRol).not.toHaveBeenCalled();
  });
  it('mantiene abierto el apartado si el servidor rechaza el rol', () => {
    const { fixture, componente, api } = crear();
    const dialogo = prepararDialogo(fixture);
    componente.abrirRolEnTrabajador();
    componente.rolForm.controls.nombre.setValue('Duplicado');
    api.guardarRol.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { mensaje: 'Ya existe un rol con ese nombre.' },
          }),
      ),
    );
    componente.guardarRol();
    fixture.detectChanges();
    expect(dialogo.hasAttribute('open')).toBe(true);
    expect(dialogo.textContent).toContain('Ya existe un rol');
    expect(componente.rolForm.controls.nombre.value).toBe('Duplicado');
  });
});
