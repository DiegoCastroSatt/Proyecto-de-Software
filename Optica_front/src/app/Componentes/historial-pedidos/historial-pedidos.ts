import { Component, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { PedidoResponse, PedidoService, ClienteOption } from '../pedido/pedido.service';

@Component({
  selector: 'app-historial-pedidos',
  imports: [DatePipe, RouterLink],
  templateUrl: './historial-pedidos.html',
  styleUrl: './historial-pedidos.css'
})
export class HistorialPedidosComponent {
  protected readonly clientes = signal<ClienteOption[]>([]);
  protected readonly clienteSeleccionado = signal<ClienteOption | null>(null);
  protected readonly pedidos = signal<PedidoResponse[]>([]);
  protected readonly rut = signal('');
  protected readonly buscandoClientes = signal(false);
  protected readonly cargandoPedidos = signal(false);
  protected readonly mensajeError = signal('');
  protected readonly errorClientes = signal(false);
  private readonly cambiosRut = new Subject<string>();

  constructor(private readonly pedidoService: PedidoService) {
    this.cambiosRut.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap(rut => {
        if (rut.length < 2) {
          this.buscandoClientes.set(false);
          return of({ clientes: [] as ClienteOption[], error: false });
        }
        this.buscandoClientes.set(true);
        this.errorClientes.set(false);
        return this.pedidoService.buscarClientesPorRut(rut).pipe(
          map(clientes => ({ clientes, error: false })),
          catchError(() => of({ clientes: [] as ClienteOption[], error: true }))
        );
      }),
      takeUntilDestroyed()
    ).subscribe(resultado => {
      this.clientes.set(resultado.clientes);
      this.errorClientes.set(resultado.error);
      this.buscandoClientes.set(false);
    });
  }

  protected cambiarRut(valor: string): void {
    this.rut.set(valor);
    this.clienteSeleccionado.set(null);
    this.pedidos.set([]);
    this.mensajeError.set('');
    this.clientes.set([]);
    const rutNormalizado = valor.replace(/[^0-9k]/gi, '').toUpperCase();
    this.buscandoClientes.set(rutNormalizado.length >= 2);
    this.cambiosRut.next(rutNormalizado);
  }

  protected seleccionarCliente(cliente: ClienteOption): void {
    this.clienteSeleccionado.set(cliente);
    this.rut.set(cliente.Rut);
    this.clientes.set([]);
    this.mensajeError.set('');
    this.cargandoPedidos.set(true);
    this.pedidoService.obtenerPedidosPorRut(cliente.Rut).subscribe({
      next: pedidos => {
        this.pedidos.set(pedidos);
        this.cargandoPedidos.set(false);
      },
      error: () => {
        this.mensajeError.set('No se pudo cargar el historial de pedidos. Inténtalo nuevamente.');
        this.cargandoPedidos.set(false);
      }
    });
  }
}
