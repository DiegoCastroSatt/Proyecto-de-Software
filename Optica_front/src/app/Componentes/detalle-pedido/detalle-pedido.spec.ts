import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { DetallePedidoComponent } from './detalle-pedido';
import { PedidoService } from '../pedido/pedido.service';

describe('DetallePedidoComponent', () => {
  let fixture: ComponentFixture<DetallePedidoComponent>;
  let servicio: {
    obtenerPedidoPorId: ReturnType<typeof vi.fn>;
    actualizarEstado: ReturnType<typeof vi.fn>;
  };

  const pedidoMock = {
    idPedido: 5,
    nombreCliente: 'Ana Pérez',
    fecha: '2026-10-01',
    estado: 'En proceso',
    total: 12000,
    anotaciones: 'Lentes antireflejo'
  };

  beforeEach(async () => {
    servicio = {
      obtenerPedidoPorId: vi.fn(() => of(pedidoMock)),
      actualizarEstado: vi.fn(() => of({ mensaje: 'Estado actualizado.', estado: 'Listo', estadoNotificacion: 'Enviada' }))
    };

    await TestBed.configureTestingModule({
      imports: [DetallePedidoComponent],
      providers: [
        { provide: PedidoService, useValue: servicio },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: () => '5' } }
          }
        },
        provideRouter([])
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
});
