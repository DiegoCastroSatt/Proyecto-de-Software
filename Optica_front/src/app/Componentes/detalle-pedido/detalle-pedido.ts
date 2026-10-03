import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { PedidoService, PedidoResponse } from '../pedido/pedido.service';

@Component({
  selector: 'app-detalle-pedido',
  imports: [RouterLink, DatePipe],
  templateUrl: './detalle-pedido.html',
  styleUrl: './detalle-pedido.css'
})
export class DetallePedidoComponent implements OnInit {
  protected readonly pedido = signal<PedidoResponse | null>(null);
  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly pedidoService: PedidoService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (isNaN(id)) {
      this.error.set('ID de pedido inválido.');
      this.cargando.set(false);
      return;
    }

    this.pedidoService.obtenerPedidoPorId(id).subscribe({
      next: (pedido) => {
        this.pedido.set(pedido);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el pedido.');
        this.cargando.set(false);
      }
    });
  }

  protected cambiarEstado(pedido: PedidoResponse, event: Event): void {
    const select = event.target as HTMLSelectElement;
    const nuevoEstado = select.value;
    
    this.pedidoService.actualizarEstado(pedido.idPedido, nuevoEstado).subscribe({
      next: (res) => {
        this.pedido.set({ ...pedido, estado: res.estado });
      },
      error: () => {
        select.value = pedido.estado;
        alert('No se pudo actualizar el estado del pedido.');
      }
    });
  }
}
