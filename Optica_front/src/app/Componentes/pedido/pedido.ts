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
    const lista = termino
      ? this.pedidos().filter(p => p.nombreCliente.toLowerCase().includes(termino))
      : this.pedidos();
    return [...lista].sort((a, b) => Number(b.vencido ?? false) - Number(a.vencido ?? false));
  });

  constructor(private readonly pedidoService: PedidoService) {}

  ngOnInit(): void {
    this.pedidoService.obtenerPedidos().subscribe({
      next: (pedidos) => this.pedidos.set(pedidos)
    });
  }
}