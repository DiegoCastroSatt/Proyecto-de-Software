import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { PedidoComponent } from './pedido';
import { PedidoService } from './pedido.service';

describe('PedidoComponent', () => {
  let fixture: ComponentFixture<PedidoComponent>;
  let servicio: { obtenerPedidos: ReturnType<typeof vi.fn> };

  const mockPedidos = [
    { idPedido: 1, nombreCliente: 'Ana Pérez', fecha: '2026-10-01', estado: 'En proceso', total: 12000 },
    { idPedido: 2, nombreCliente: 'Carlos Soto', fecha: '2026-10-02', estado: 'Listo', total: 25000 }
  ];

  beforeEach(async () => {
    servicio = {
      obtenerPedidos: vi.fn(() => of(mockPedidos))
    };
    await TestBed.configureTestingModule({
      imports: [PedidoComponent],
      providers: [{ provide: PedidoService, useValue: servicio }, provideRouter([])]
    }).compileComponents();
    fixture = TestBed.createComponent(PedidoComponent);
    fixture.detectChanges();
  });

  it('carga y muestra la lista de pedidos al inicializar', () => {
    expect(servicio.obtenerPedidos).toHaveBeenCalledTimes(1);
    const tarjetas = fixture.nativeElement.querySelectorAll('.pedido-card');
    expect(tarjetas.length).toBe(2);
    expect(fixture.nativeElement.textContent).toContain('Ana Pérez');
    expect(fixture.nativeElement.textContent).toContain('Carlos Soto');
  });

  it('filtra los pedidos por nombre de cliente con el buscador', () => {
    const input = fixture.nativeElement.querySelector('.search-input') as HTMLInputElement;
    input.value = 'carlos';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const tarjetas = fixture.nativeElement.querySelectorAll('.pedido-card');
    expect(tarjetas.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Carlos Soto');
    expect(fixture.nativeElement.textContent).not.toContain('Ana Pérez');
  });

  it('muestra estado vacío cuando la búsqueda no coincide con ningún cliente', () => {
    const input = fixture.nativeElement.querySelector('.search-input') as HTMLInputElement;
    input.value = 'inexistente';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const tarjetas = fixture.nativeElement.querySelectorAll('.pedido-card');
    expect(tarjetas.length).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('Sin pedidos');
  });

  it('cada tarjeta de pedido incluye el enlace al detalle', () => {
    const enlaces = fixture.nativeElement.querySelectorAll('.btn-detalle');
    expect(enlaces.length).toBe(2);
    expect(enlaces[0].getAttribute('href')).toBe('/admin/panel/pedido/1');
  });
});
