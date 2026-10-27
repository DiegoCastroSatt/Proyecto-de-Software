import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { HistorialPedidosComponent } from './historial-pedidos';
import { PedidoService } from '../pedido/pedido.service';

describe('HistorialPedidosComponent', () => {
  let fixture: ComponentFixture<HistorialPedidosComponent>;
  let servicio: {
    buscarClientesPorRut: ReturnType<typeof vi.fn>;
    obtenerPedidosPorRut: ReturnType<typeof vi.fn>;
  };

  const cliente = { Rut: '12.345.678-5', nombreCompleto: 'Ana Pérez' };
  const pedidos = [
    { idPedido: 12, nombreCliente: 'Ana Pérez', fecha: '2026-10-01', estado: 'Entregado', total: 12000 },
    { idPedido: 8, nombreCliente: 'Ana Pérez', fecha: '2026-08-01', estado: 'Entregado', total: 8000 }
  ];

  beforeEach(async () => {
    servicio = {
      buscarClientesPorRut: vi.fn(() => of([cliente])),
      obtenerPedidosPorRut: vi.fn(() => of(pedidos))
    };
    await TestBed.configureTestingModule({
      imports: [HistorialPedidosComponent],
      providers: [{ provide: PedidoService, useValue: servicio }, provideRouter([])]
    }).compileComponents();

    vi.useFakeTimers();
    fixture = TestBed.createComponent(HistorialPedidosComponent);
    fixture.detectChanges();
  });

  afterEach(() => vi.useRealTimers());

  it('busca clientes por RUT con autocompletado y muestra sus pedidos y detalle', async () => {
    const input = fixture.nativeElement.querySelector('#rut-cliente') as HTMLInputElement;
    input.value = '12.345';
    input.dispatchEvent(new Event('input'));
    await vi.advanceTimersByTimeAsync(250);
    fixture.detectChanges();

    expect(servicio.buscarClientesPorRut).toHaveBeenCalledWith('12345');
    const sugerencia = fixture.nativeElement.querySelector('[role="option"]') as HTMLButtonElement;
    expect(sugerencia.textContent).toContain('Ana Pérez');
    expect(sugerencia.textContent).toContain('12.345.678-5');
    sugerencia.click();
    fixture.detectChanges();

    expect(servicio.obtenerPedidosPorRut).toHaveBeenCalledWith('12.345.678-5');
    expect(fixture.nativeElement.textContent).toContain('Pedidos de Ana Pérez');
    expect(fixture.nativeElement.querySelectorAll('.pedido-historico').length).toBe(2);
    expect(fixture.nativeElement.querySelector('.pedido-historico a').getAttribute('href'))
      .toBe('/admin/panel/pedido/12');
  });

  it('muestra un estado vacío si el cliente no registra pedidos', async () => {
    servicio.obtenerPedidosPorRut.mockReturnValueOnce(of([]));
    fixture.componentInstance['seleccionarCliente'](cliente);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Este cliente no tiene pedidos registrados.');
  });

  it('informa los errores de consulta del historial', () => {
    servicio.obtenerPedidosPorRut.mockReturnValueOnce(throwError(() => new Error('offline')));
    fixture.componentInstance['seleccionarCliente'](cliente);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent)
      .toContain('No se pudo cargar el historial de pedidos.');
  });
});
