import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PedidoComponent } from './pedido';
import { PedidoService } from './pedido.service';

describe('PedidoComponent', () => {
  let fixture: ComponentFixture<PedidoComponent>;
  let servicio: { obtenerPedidos: ReturnType<typeof vi.fn>; actualizarEstado: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    servicio = {
      obtenerPedidos: vi.fn(() => of([{ idPedido: 5, nombreCliente: 'Ana Pérez', fecha: '2026-10-01', estado: 'En proceso', total: 12000 }])),
      actualizarEstado: vi.fn(() => of({ mensaje: 'Estado actualizado.', estado: 'Listo', estadoNotificacion: 'Enviada' }))
    };
    await TestBed.configureTestingModule({
      imports: [PedidoComponent],
      providers: [{ provide: PedidoService, useValue: servicio }, provideRouter([])]
    }).compileComponents();
    fixture = TestBed.createComponent(PedidoComponent);
    fixture.detectChanges();
  });

  it('cancelar la confirmación no cambia el estado ni envía correo', () => {
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

  it('confirma el pedido listo y muestra éxito solo cuando la API confirma el correo', () => {
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

  it('avisa si el estado se actualiza pero el correo falla', () => {
    servicio.actualizarEstado.mockReturnValueOnce(of({ mensaje: 'Correo fallido', estado: 'Listo', estadoNotificacion: 'Fallida' }));
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'Listo';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.pedido-confirmacion-acciones .btn-primary') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.pedido-toast').textContent).toContain('El pedido se actualizó correctamente, pero no se pudo enviar el correo al cliente.');
  });

  it('no muestra un aviso de correo para otros estados', () => {
    servicio.actualizarEstado.mockReturnValueOnce(of({ mensaje: 'Estado actualizado.', estado: 'Entregado' }));
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'Entregado';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.pedido-toast')).toBeNull();
  });

  it('informa si falla la actualización del estado', () => {
    servicio.actualizarEstado.mockReturnValueOnce(throwError(() => new Error('offline')));
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = 'Entregado';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.pedido-toast').textContent).toContain('No se pudo actualizar el estado del pedido');
  });
});
