import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PedidoService, PedidoResponse } from '../pedido/pedido.service';

@Component({
  selector: 'app-consultar-pedido',
  imports: [DatePipe, RouterLink],
  templateUrl: './consultar-pedido.html',
  styleUrl: './consultar-pedido.css'
})
export class ConsultarPedidoComponent implements OnInit {
  /** RUT del cliente con sesión activa. null = sin sesión. */
  protected readonly rutCliente = signal<string | null>(null);

  protected readonly pedidos = signal<PedidoResponse[]>([]);
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor(private readonly pedidoService: PedidoService) {}

  ngOnInit(): void {
    // TODO: obtener el RUT del servicio de autenticación de cliente cuando esté implementado.
    // Ejemplo: this.rutCliente.set(this.authService.getRutCliente());
    const rut = this.obtenerRutSesion();
    this.rutCliente.set(rut);

    if (rut) {
      this.cargarPedidos(rut);
    }
  }

  /**
   * Stub que simula la obtención del RUT del cliente con sesión.
   * Retorna null porque aún no existe la funcionalidad de login de cliente.
   */
  private obtenerRutSesion(): string | null {
    return null;
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
