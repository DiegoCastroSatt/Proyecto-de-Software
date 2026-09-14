import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CrearPedido {
  cliente: string;
  receta: string;
  fecha: string;
  estado: string; // 'Pendiente'
}

export interface PedidoResponse {
  id: number;
  cliente: string;
  receta: string;
  fecha: string;
  estado: string;
}

@Injectable({ providedIn: 'root' })
export class PedidoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Pedidos'; // Asumiendo esta ruta

  crearPedido(pedido: CrearPedido): Observable<PedidoResponse> {
    return this.http.post<PedidoResponse>(this.apiUrl, pedido);
  }
}
