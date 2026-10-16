import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { DetallePedidoComponent } from './detalle-pedido';
import { PedidoService } from '../pedido/pedido.service';

describe('DetallePedidoComponent', () => {
  let fixture: ComponentFixture<DetallePedidoComponent>;
  let servicio: {
    obtenerPedidoPorId: ReturnType<typeof vi.fn>;
    actualizarEstado: ReturnType<typeof vi.fn>;
  };
  let idRuta: string;

  const pedidoMock = {
    idPedido: 5,
    nombreCliente: 'Ana Pérez',
    fecha: '2026-10-01',
    estado: 'En proceso',
    total: 12000,
    anotaciones: 'Lentes antireflejo'
  };

  beforeEach(async () => {
    idRuta = '5';
    servicio = {
      obtenerPedidoPorId: vi.fn(() => of(pedidoMock)),
      actualizarEstado: vi.fn(() => of({ mensaje: 'Estado actualizado.', estado: 'Listo', estadoNotificacion: 'Enviada' }))
    };

    await TestBed.configureTestingModule({
      imports: [DetallePedidoComponent],
      providers: [
        provideRouter([]),
        { provide: PedidoService, useValue: servicio },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: () => idRuta } }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DetallePedidoComponent);
    fixture.detectChanges();
  });

  it('carga y muestra la información del pedido correctamente', () => {
    expect(servicio.obtenerPedidoPorId).toHaveBeenCalledWith(5);
    expect(fixture.nativeElement.textContent).toContain('Ana Pérez');
    expect(fixture.nativeElement.textContent).toContain('12000');
    expect(fixture.nativeElement.textContent).toContain('Lentes antireflejo');
  });

  it('rechaza un identificador no numérico sin consultar la API', () => {
    idRuta = 'pedido-invalido';
    servicio.obtenerPedidoPorId.mockClear();

    fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(servicio.obtenerPedidoPorId).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.error-title').textContent)
      .toContain('ID de pedido inválido.');
    expect(fixture.nativeElement.querySelector('.loading-state')).toBeNull();
  });

  it('muestra el error de carga cuando la API falla', () => {
    servicio.obtenerPedidoPorId.mockReturnValueOnce(
      throwError(() => new Error('offline'))
    );
    servicio.obtenerPedidoPorId.mockClear();

    fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(servicio.obtenerPedidoPorId).toHaveBeenCalledWith(5);
    expect(fixture.nativeElement.querySelector('.error-title').textContent)
      .toContain('No se pudo cargar el pedido.');
    expect(fixture.nativeElement.querySelector('.pedido-detail-card')).toBeNull();
  });

  it('informa cuando el pedido no tiene anotaciones', () => {
    servicio.obtenerPedidoPorId.mockReturnValueOnce(of({
      ...pedidoMock,
      anotaciones: undefined
    }));
    servicio.obtenerPedidoPorId.mockClear();

    fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent)
      .toContain('Sin anotaciones para este pedido.');
  });

  it('cancelar la confirmación de "Listo" no cambia el estado ni envía correo', () => {
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'Listo';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(servicio.actualizarEstado).not.toHaveBeenCalled();
    expect(select.value).toBe('En proceso');
    const cancelar = fixture.nativeElement.querySelector('.pedido-confirmacion-cancelar') as HTMLButtonElement;
    cancelar.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).toBeNull();
    expect(servicio.actualizarEstado).not.toHaveBeenCalled();
  });

  it('confirma el pedido listo y muestra éxito cuando la API confirma el envío de correo', () => {
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'Listo';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).not.toBeNull();
    expect(servicio.actualizarEstado).not.toHaveBeenCalled();

    const confirmar = fixture.nativeElement.querySelector('.pedido-confirmacion-acciones .btn-primary') as HTMLButtonElement;
    confirmar.click();
    fixture.detectChanges();

    expect(servicio.actualizarEstado).toHaveBeenCalledWith(5, 'Listo');
    expect(fixture.nativeElement.querySelector('.pedido-toast').textContent).toContain('El pedido se actualizó y se envió el correo al cliente exitosamente.');
  });

  it('muestra advertencia cuando el estado se actualiza pero el envío de correo falla', () => {
    servicio.actualizarEstado.mockReturnValueOnce(of({ mensaje: 'Correo fallido', estado: 'Listo', estadoNotificacion: 'Fallida' }));
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'Listo';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.pedido-confirmacion-acciones .btn-primary') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.pedido-toast').textContent).toContain('El pedido se actualizó correctamente, pero no se pudo enviar el correo al cliente.');
  });

  it('no muestra aviso de correo para estados diferentes a "Listo"', () => {
    servicio.actualizarEstado.mockReturnValueOnce(of({ mensaje: 'Estado actualizado.', estado: 'Entregado' }));
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'Entregado';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.pedido-toast')).toBeNull();
  });

  it('informa si falla la actualización del estado desde la API', () => {
    servicio.actualizarEstado.mockReturnValueOnce(throwError(() => new Error('offline')));
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'Entregado';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.pedido-toast').textContent).toContain('No se pudo actualizar el estado del pedido');
  });

  it('muestra el mensaje de error del servidor y restaura el estado anterior', () => {
    servicio.actualizarEstado.mockReturnValueOnce(
      throwError(() => ({ error: { mensaje: 'Transición de estado no permitida.' } }))
    );
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'Entregado';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(select.value).toBe('En proceso');
    expect(select.disabled).toBe(false);
    expect(fixture.nativeElement.querySelector('.pedido-toast').textContent)
      .toContain('Transición de estado no permitida.');
  });

  it('bloquea cambios simultáneos y habilita el control al finalizar la actualización', () => {
    const respuesta = new Subject<{ mensaje: string; estado: string }>();
    servicio.actualizarEstado.mockReturnValueOnce(respuesta);
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;

    select.value = 'Entregado';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(servicio.actualizarEstado).toHaveBeenCalledTimes(1);
    expect(select.disabled).toBe(true);

    select.value = 'Cancelado';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(servicio.actualizarEstado).toHaveBeenCalledTimes(1);
    expect(select.value).toBe('En proceso');

    respuesta.next({ mensaje: 'Estado actualizado.', estado: 'Entregado' });
    respuesta.complete();
    fixture.detectChanges();

    expect(select.disabled).toBe(false);
    expect(select.value).toBe('Entregado');
  });
});
