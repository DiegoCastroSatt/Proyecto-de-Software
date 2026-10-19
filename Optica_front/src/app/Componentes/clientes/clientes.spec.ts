import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ClientesComponent } from './clientes';
import { ClienteService, Cliente, HistorialCliente } from './cliente.service';

describe('ClientesComponent', () => {
  let component: ClientesComponent;
  let fixture: ComponentFixture<ClientesComponent>;
  let clienteServiceMock: any;

  const mockClientes: Cliente[] = [
    {
      idCliente: 1,
      rut: '12345678-5',
      nombre: 'María',
      apellido: 'González',
      telefono: '+56912345678',
      correo: 'maria@test.cl',
      estado: 'Activo',
      fechaRegistro: '2026-01-01'
    }
  ];

  const mockHistorial: HistorialCliente = {
    idCliente: 1,
    rut: '12345678-5',
    nombreCompleto: 'María González',
    correo: 'maria@test.cl',
    telefono: '+56912345678',
    estado: 'Activo',
    recetas: [
      {
        idReceta: 10,
        fecha: '2026-03-01',
        observaciones: 'Control anual',
        graduaciones: [
      { ojo: 'OD', esfera: -1.5, cilindro: -0.5, eje: 90 },
      { ojo: 'OI', esfera: -1.25, cilindro: -0.25, eje: 85 }
    ]
      }
    ],
    pedidos: [
      {
        idPedido: 101,
        fecha: '2026-03-02',
        estado: 'Entregado',
        total: 89990,
        idReceta: 10,
        anotaciones: 'Entregado con estuche'
      }
    ]
  };

  beforeEach(async () => {
    clienteServiceMock = {
      buscar: vi.fn().mockReturnValue(of(mockClientes)),
      registrar: vi.fn().mockReturnValue(of(mockClientes[0])),
      actualizar: vi.fn().mockReturnValue(of(mockClientes[0])),
      cambiarEstado: vi.fn().mockReturnValue(of({ mensaje: 'Ok' })),
      obtenerHistorial: vi.fn().mockReturnValue(of(mockHistorial))
    };

    await TestBed.configureTestingModule({
      imports: [ClientesComponent],
      providers: [
        { provide: ClienteService, useValue: clienteServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClientesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('debe crear el componente e inicializar con la lista de clientes', () => {
    expect(component).toBeTruthy();
    expect(clienteServiceMock.buscar).toHaveBeenCalled();
    expect(component.clientes.length).toBe(1);
    expect(component.clientes[0].nombre).toBe('María');
  });

  it('debe marcar el formulario como inválido si el RUT no tiene dígito verificador correcto', () => {
    const rutControl = component.clienteForm.get('rut');
    rutControl?.setValue('12345678-9'); // DV incorrecto
    expect(rutControl?.hasError('rutInvalido')).toBe(true);
    expect(component.clienteForm.invalid).toBe(true);
  });

  it('debe marcar el formulario como inválido si falta tanto el teléfono como el correo', () => {
    component.clienteForm.patchValue({
      rut: '12.345.678-5',
      nombre: 'Juan',
      apellido: 'Pérez',
      telefono: '',
      correo: ''
    });
    expect(component.clienteForm.errors?.['requireContact']).toBe(true);
  });

  it('debe registrar un cliente si el formulario es válido', () => {
    component.clienteForm.patchValue({
      rut: '12.345.678-5',
      nombre: 'Ana',
      apellido: 'Pérez',
      telefono: '+56912345678',
      correo: 'ana@test.cl'
    });

    component.registrar();

    expect(clienteServiceMock.registrar).toHaveBeenCalled();
    expect(component.toast?.tipo).toBe('success');
  });

  it('debe mostrar aviso de conflicto (409) si el cliente ya existe', () => {
    clienteServiceMock.registrar.mockReturnValue(
      throwError(() => ({ status: 409, error: { mensaje: 'RUT duplicado' } }))
    );

    component.clienteForm.patchValue({
      rut: '12.345.678-5',
      nombre: 'Ana',
      apellido: 'Pérez',
      telefono: '+56912345678',
      correo: 'ana@test.cl'
    });

    component.registrar();

    expect(component.toast?.tipo).toBe('error');
    expect(component.toast?.titulo).toBe('Dato duplicado');
  });

  it('debe abrir y poblar el modal de edición de cliente', () => {
    const cliente = mockClientes[0];
    component.abrirModalEdicion(cliente);

    expect(component.modalEdicionAbierto).toBe(true);
    expect(component.editarForm.get('nombre')?.value).toBe('María');
    expect(component.editarForm.get('rut')?.disabled).toBe(true);
  });

  it('debe abrir modal de cambio de estado y alternar a Inactivo', () => {
    const cliente = mockClientes[0];
    component.abrirModalEstado(cliente, 'Inactivo');

    expect(component.modalEstadoAbierto).toBe(true);
    expect(component.nuevoEstadoObjetivo).toBe('Inactivo');

    component.confirmarCambioEstado();

    expect(clienteServiceMock.cambiarEstado).toHaveBeenCalledWith(1, 'Inactivo');
    expect(component.modalEstadoAbierto).toBe(false);
  });

  it('debe abrir el modal de historial y cargar recetas y pedidos', () => {
    const cliente = mockClientes[0];
    component.abrirModalHistorial(cliente);

    expect(component.modalHistorialAbierto).toBe(true);
    expect(clienteServiceMock.obtenerHistorial).toHaveBeenCalledWith(1);
    expect(component.historialSeleccionado?.recetas.length).toBe(1);
    expect(component.historialSeleccionado?.pedidos.length).toBe(1);
    expect(component.pestanaActiva).toBe('recetas');
  });

  it('debe permitir cambiar de pestaña a pedidos y cerrar el modal de historial', () => {
    component.abrirModalHistorial(mockClientes[0]);
    component.cambiarPestana('pedidos');

    expect(component.pestanaActiva).toBe('pedidos');

    component.cerrarModalHistorial();
    expect(component.modalHistorialAbierto).toBe(false);
    expect(component.historialSeleccionado).toBeNull();
  });
});