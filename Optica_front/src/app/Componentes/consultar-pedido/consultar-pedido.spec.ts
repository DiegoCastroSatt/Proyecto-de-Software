import '@angular/compiler';
import { ComponentFixture, TestBed, getTestBed } from '@angular/core/testing';
import { BrowserTestingModule, platformBrowserTesting } from '@angular/platform-browser/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { ConsultarPedidoComponent } from './consultar-pedido';
import { PedidoService } from '../pedido/pedido.service';
import { AutenticacionClienteService, ClienteSesion } from '../login-cliente/autenticacion-cliente.service';

try {
  getTestBed().initTestEnvironment(BrowserTestingModule, platformBrowserTesting());
} catch {
  // Ya inicializado
}

describe('ConsultarPedidoComponent', () => {
  let fixture: ComponentFixture<ConsultarPedidoComponent>;
  let pedidoServiceMock: {
    obtenerPedidosPorRut: ReturnType<typeof vi.fn>;
  };
  let authServiceMock: {
    cliente: ReturnType<typeof signal<ClienteSesion | null>>;
    verificarSesion: ReturnType<typeof vi.fn>;
  };

  const pedidosMock = [
    {
      idPedido: 101,
      nombreCliente: 'Tomás Marambio',
      fecha: '2026-10-25T14:30:00',
      estado: 'Pendiente',
      total: 35000,
      anotaciones: 'Filtro azul'
    },
    {
      idPedido: 102,
      nombreCliente: 'Tomás Marambio',
      fecha: '2026-10-20T10:00:00',
      estado: 'Listo',
      total: 50000
    }
  ];

  beforeEach(() => {
    pedidoServiceMock = {
      obtenerPedidosPorRut: vi.fn(() => of(pedidosMock))
    };
    authServiceMock = {
      cliente: signal<ClienteSesion | null>({
        idCliente: 9,
        nombre: 'Tomás Marambio',
        correo: 'tomas.marambio1@gmail.com',
        rut: '206608730'
      }),
      verificarSesion: vi.fn(() => of({
        idCliente: 9,
        nombre: 'Tomás Marambio',
        correo: 'tomas.marambio1@gmail.com',
        rut: '206608730'
      }))
    };
  });

  const crearComponente = async () => {
    await TestBed.configureTestingModule({
      imports: [ConsultarPedidoComponent],
      providers: [
        { provide: PedidoService, useValue: pedidoServiceMock },
        { provide: AutenticacionClienteService, useValue: authServiceMock },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ConsultarPedidoComponent);
    fixture.detectChanges();
  };

  it('consulta pedidos usando el RUT de la sesión cuando el cliente está autenticado', async () => {
    await crearComponente();

    expect(pedidoServiceMock.obtenerPedidosPorRut).toHaveBeenCalledWith('206608730');
    expect(fixture.nativeElement.querySelector('.pedidos-section')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.no-session')).toBeNull();
  });

  it('muestra la sección sin sesión cuando no hay cliente autenticado', async () => {
    authServiceMock.cliente.set(null);
    await crearComponente();

    expect(fixture.nativeElement.querySelector('.no-session')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.pedidos-section')).toBeNull();
  });

  it('muestra el mensaje "No tiene pedidos activos" cuando el cliente no registra pedidos', async () => {
    pedidoServiceMock.obtenerPedidosPorRut = vi.fn(() => of([]));
    await crearComponente();

    const emptyTitle = fixture.nativeElement.querySelector('.empty-title');
    expect(emptyTitle).not.toBeNull();
    expect(emptyTitle.textContent).toContain('No tiene pedidos activos');
  });

  it('muestra la fecha y el estado de los pedidos sin importar el estado', async () => {
    await crearComponente();

    const cards = fixture.nativeElement.querySelectorAll('.pedido-card');
    expect(cards.length).toBe(2);

    const primerPedidoTexto = cards[0].textContent;
    expect(primerPedidoTexto).toContain('Pendiente');
    expect(primerPedidoTexto).toContain('25/10/2026');

    const segundoPedidoTexto = cards[1].textContent;
    expect(segundoPedidoTexto).toContain('Listo');
    expect(segundoPedidoTexto).toContain('20/10/2026');
  });

  it('muestra una lista de pedidos cuando el cliente tiene más de un pedido', async () => {
    await crearComponente();

    const lista = fixture.nativeElement.querySelector('.pedidos-list');
    expect(lista).not.toBeNull();
    const items = fixture.nativeElement.querySelectorAll('.pedido-card');
    expect(items.length).toBe(2);
  });
});
