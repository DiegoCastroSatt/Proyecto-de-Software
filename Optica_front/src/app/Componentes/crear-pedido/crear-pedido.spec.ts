import { AutocompletadoRutService } from '../../shared/components/autocompletado-rut/autocompletado-rut.service';
import { AutocompletadoRut } from '../../shared/components/autocompletado-rut/autocompletado-rut';
import { By } from '@angular/platform-browser';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CrearPedidoComponent } from './crear-pedido';
import { PedidoService } from '../pedido/pedido.service';
import { crearCotizacionPedidoPdf } from './cotizacion-pedido-pdf';

describe('CrearPedidoComponent', () => {
  let fixture: ComponentFixture<CrearPedidoComponent>;
  let servicio: {
    obtenerClientes: ReturnType<typeof vi.fn>;
    crearPedido: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  const clientes = [
    { Rut: '12345678-9', nombreCompleto: 'Ana Pérez' },
    { Rut: '98765432-1', nombreCompleto: 'Carlos Soto' }
  ];

  beforeEach(async () => {
    servicio = {
      obtenerClientes: vi.fn(() => of(clientes)),
      crearPedido: vi.fn(() => of({ idPedido: 1 }))
    };

    await TestBed.configureTestingModule({
      imports: [CrearPedidoComponent],
      providers: [{ provide: AutocompletadoRutService, useValue: { buscarSugerencias: vi.fn(() => of([])) } },
        { provide: PedidoService, useValue: servicio },
        provideRouter([])
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate');
    fixture = TestBed.createComponent(CrearPedidoComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('conecta la sugerencia seleccionada con el RUT enviado al crear el pedido', () => {
    const control = fixture.debugElement.query(By.directive(AutocompletadoRut)).componentInstance as AutocompletadoRut;
    control.seleccionarSugerencia({ rut: '12.345.678-5', nombre: 'Ana', apellido: 'Pérez' });
    fixture.detectChanges();
    expect(fixture.componentInstance['pedidoForm'].controls.rut.value).toBe('12.345.678-5');
    fixture.componentInstance['pedidoForm'].patchValue({ fecha: '2026-10-23', total: 1000 });
    fixture.componentInstance['submitPedido']();
    expect(servicio.crearPedido).toHaveBeenCalledWith(expect.objectContaining({ rut: '12.345.678-5' }));
    fixture.detectChanges(); expect(control.value()).toBe('');
  });

  it('carga los clientes al iniciar', () => {
    expect(servicio.obtenerClientes).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance['clientes']()).toEqual(clientes);
  });

  it('no crea un pedido si el formulario es inválido', () => {
    const formulario = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    formulario.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(servicio.crearPedido).not.toHaveBeenCalled();
    expect(fixture.componentInstance['pedidoForm'].touched).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('El RUT es obligatorio');
    expect(fixture.nativeElement.textContent).toContain('La fecha es obligatoria');
  });

  it('crea un pedido válido, limpia el formulario y vuelve a la lista', () => {
    vi.useFakeTimers();
    fixture.componentInstance['pedidoForm'].setValue({
      rut: '12345678-9',
      fecha: '2026-10-14',
      estado: 'En proceso',
      productos: 'Armazón Ray-Ban',
      total: 12000,
      anotaciones: 'Lentes antireflejo'
    });

    const formulario = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    formulario.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(servicio.crearPedido).toHaveBeenCalledWith({
      rut: '12345678-9',
      fecha: '2026-10-14',
      estado: 'En proceso',
      total: 12000,
      anotaciones: 'Productos:\nArmazón Ray-Ban\n\nAnotaciones:\nLentes antireflejo'
    });
    expect(fixture.nativeElement.textContent).toContain('Pedido guardado exitosamente.');
    expect(fixture.componentInstance['pedidoForm'].pristine).toBe(true);

    vi.advanceTimersByTime(2000);
    expect(router.navigate).toHaveBeenCalledWith(['/admin/panel/pedido']);
  });

  it('muestra el mensaje devuelto por la API cuando falla la creación', () => {
    servicio.crearPedido.mockReturnValueOnce(
      throwError(() => ({ error: { mensaje: 'El cliente no existe.' } }))
    );
    fixture.componentInstance['pedidoForm'].setValue({
      rut: '12345678-9',
      fecha: '2026-10-14',
      estado: 'Pendiente',
      productos: '',
      total: 0,
      anotaciones: ''
    });

    fixture.componentInstance['submitPedido']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error-banner').textContent)
      .toContain('El cliente no existe.');
    expect(fixture.nativeElement.querySelector('.success-banner')).toBeNull();
  });

  it('muestra un error si no se pueden cargar los clientes', () => {
    servicio.obtenerClientes.mockReturnValueOnce(
      throwError(() => new Error('offline'))
    );
    fixture.destroy();
    fixture = TestBed.createComponent(CrearPedidoComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent)
      .toContain('No se pudieron cargar los clientes.');
  });

  it('exige el RUT del cliente para generar la cotización', async () => {
    fixture.componentInstance['pedidoForm'].patchValue({ rut: '' });
    await fixture.componentInstance['generarCotizacion']();
    fixture.detectChanges();

    expect(fixture.componentInstance['errorMessage']())
      .toBe('Ingresa el RUT del cliente para generar la cotización.');
  });

  it('genera y abre la vista previa superpuesta de la cotización sin descargar automáticamente', async () => {
    const crearUrl = vi.fn((_blob: Blob) => 'blob:cotizacion');
    const revocarUrl = vi.fn();
    vi.stubGlobal('URL', class extends URL {
      static override createObjectURL = crearUrl;
      static override revokeObjectURL = revocarUrl;
    });

    const clic = vi.spyOn(HTMLAnchorElement.prototype, 'click');

    fixture.componentInstance['pedidoForm'].setValue({
      rut: '12345678-9',
      fecha: '2026-11-10',
      estado: 'Pendiente',
      productos: 'Armazón Ray-Ban Aviador',
      total: 45000,
      anotaciones: 'Cristales fotocromáticos con filtro azul'
    });

    await fixture.componentInstance['generarCotizacion']();
    fixture.detectChanges();

    // No debe descargar automáticamente al generar
    expect(clic).not.toHaveBeenCalled();
    expect(crearUrl).toHaveBeenCalled();
    expect(fixture.componentInstance['mostrarModalCotizacion']()).toBe(true);
    expect(fixture.componentInstance['pdfPreviewUrl']()).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.modal-cotizacion')).not.toBeNull();
  });

  it('permite descargar el PDF desde el modal y finalizar el pedido sin descargar', async () => {
    const crearUrl = vi.fn((_blob: Blob) => 'blob:cotizacion');
    const revocarUrl = vi.fn();
    vi.stubGlobal('URL', class extends URL {
      static override createObjectURL = crearUrl;
      static override revokeObjectURL = revocarUrl;
    });

    let nombreDescarga = '';
    const clic = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function(this: HTMLAnchorElement) {
      nombreDescarga = this.download;
    });

    fixture.componentInstance['pedidoForm'].setValue({
      rut: '12345678-9',
      fecha: '2026-11-10',
      estado: 'Pendiente',
      productos: 'Armazón Oakley Metal',
      total: 45000,
      anotaciones: 'Cristales fotocromáticos con filtro azul'
    });

    await fixture.componentInstance['generarCotizacion']();
    fixture.detectChanges();

    // Descarga explícita desde el botón del modal
    fixture.componentInstance['descargarCotizacion']();
    expect(clic).toHaveBeenCalledOnce();
    expect(nombreDescarga).toBe('cotizacion-pedido-123456789.pdf');

    // Finalizar pedido desde el modal guarda el pedido y cierra el modal
    fixture.componentInstance['finalizarPedidoDesdeModal']();
    expect(fixture.componentInstance['mostrarModalCotizacion']()).toBe(false);
    expect(servicio.crearPedido).toHaveBeenCalledWith(expect.objectContaining({
      rut: '12345678-9',
      total: 45000,
      anotaciones: expect.stringContaining('Armazón Oakley Metal')
    }));
  });

  it('crea un PDF válido con la estructura de cotización, productos y disclaimers', async () => {
    const pdf = await crearCotizacionPedidoPdf({
      rutCliente: '12.345.678-9',
      nombreCliente: 'Ana Pérez',
      fecha: '2026-11-10',
      total: 89990,
      productos: 'Armazón Ray-Ban Aviador RB3025 + Cristales Antirreflejo',
      anotaciones: 'Armazón metálico liviano'
    });

    const salida = pdf.output();
    expect(salida.startsWith('%PDF-')).toBe(true);
    expect(salida).toContain('89.990');
    expect(salida).toContain('12.345.678-9');
    expect(salida).toContain('Ana Pérez');
    expect(salida).toContain('COTIZACIÓN DE PEDIDO');
    expect(salida).toContain('PRODUCTOS / DETALLE COTIZADO');
    expect(salida).toContain('Armazón Ray-Ban Aviador RB3025');
    expect(pdf.getNumberOfPages()).toBe(1);
  });
  });
});
