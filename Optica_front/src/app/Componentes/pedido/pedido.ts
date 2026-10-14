import { Component, OnInit, signal, computed } from '@angular/core';
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
  protected readonly terminoBusqueda = signal<string>('');

  protected readonly pedidosFiltrados = computed(() => {
    const termino = this.terminoBusqueda().toLowerCase().trim();
    if (!termino) {
      return this.pedidos();
    }
    return this.pedidos().filter(p => p.nombreCliente.toLowerCase().includes(termino));
  });

  constructor(private readonly pedidoService: PedidoService) {}

  ngOnInit(): void {
    this.pedidoService.obtenerPedidos().subscribe({
      next: (pedidos) => this.pedidos.set(pedidos)
    });
  }
}
