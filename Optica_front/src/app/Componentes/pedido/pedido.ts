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
}
