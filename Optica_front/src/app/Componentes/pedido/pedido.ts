import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PedidoService, PedidoResponse } from './pedido.service';

@Component({
  selector: 'app-pedido',
  imports: [RouterLink, DatePipe],
  templateUrl: './pedido.html',
  styleUrl: './pedido.css'
})
export class PedidoComponent implements OnInit {
  protected readonly pedidos = signal<PedidoResponse[]>([]);

  constructor(private readonly pedidoService: PedidoService) {}

  ngOnInit(): void {
    this.pedidoService.obtenerPedidos().subscribe({
      next: (pedidos) => this.pedidos.set(pedidos)
    });
  }

  protected cambiarEstado(pedido: PedidoResponse, event: Event): void {
    const select = event.target as HTMLSelectElement;
    const nuevoEstado = select.value;
    
    this.pedidoService.actualizarEstado(pedido.idPedido, nuevoEstado).subscribe({
      next: (res) => {
        this.pedidos.update(pedidos => 
          pedidos.map(p => p.idPedido === pedido.idPedido ? { ...p, estado: res.estado } : p)
        );
      },
      error: () => {
        select.value = pedido.estado;
        alert('No se pudo actualizar el estado del pedido.');
      }
    });
  }
}
