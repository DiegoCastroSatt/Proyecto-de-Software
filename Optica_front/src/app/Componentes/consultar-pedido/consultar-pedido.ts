import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { PedidoService, PedidoResponse } from '../pedido/pedido.service';
import { AutenticacionClienteService } from '../login-cliente/autenticacion-cliente.service';

@Component({
  selector: 'app-consultar-pedido',
  imports: [DatePipe],
  templateUrl: './consultar-pedido.html',
  styleUrl: './consultar-pedido.css'
})
export class ConsultarPedidoComponent implements OnInit {
  private readonly pedidoService = inject(PedidoService);
  private readonly authService = inject(AutenticacionClienteService);

  /** RUT del cliente con sesión activa. null = sin sesión. */
  protected readonly rutCliente = signal<string | null>(null);
  protected readonly pedidos = signal<PedidoResponse[]>([]);
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const sesionCliente = this.authService.cliente();
    if (sesionCliente?.rut) {
      this.rutCliente.set(sesionCliente.rut);
      this.cargarPedidos(sesionCliente.rut);
    } else {
      this.rutCliente.set(null);
    }
  }

  private cargarPedidos(rut: string): void {
    this.cargando.set(true);
    this.error.set(null);

    this.pedidoService.obtenerPedidosPorRut(rut).subscribe({
      next: (pedidos) => {
        this.pedidos.set(pedidos);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar tus pedidos. Intenta nuevamente.');
        this.cargando.set(false);
      }
    });
  }
}

