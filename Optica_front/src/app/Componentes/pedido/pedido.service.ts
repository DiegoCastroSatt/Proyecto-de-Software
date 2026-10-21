import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CrearPedido {
  rut: string;
  fecha: string;
  estado: string;
  total: number;
  anotaciones?: string;
}

export interface PedidoResponse {
  idPedido: number;
  nombreCliente: string;
  fecha: string;
  estado: string;
  total: number;
  anotaciones?: string;
  estadoNotificacion?: string;
}

export interface ClienteOption {
  Rut: string;
  nombreCompleto: string;
}

@Injectable({ providedIn: 'root' })
export class PedidoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Pedidos';

  obtenerPedidos(): Observable<PedidoResponse[]> {
    return this.http.get<PedidoResponse[]>(this.apiUrl);
  }

  obtenerClientes(): Observable<ClienteOption[]> {
    return this.http.get<ClienteOption[]>(`${this.apiUrl}/clientes`);
  }

  buscarClientesPorRut(rut: string): Observable<ClienteOption[]> {
    return this.http.get<ClienteOption[]>(`${this.apiUrl}/clientes/buscar`, { params: { rut } });
  }

  obtenerPedidosPorRut(rut: string): Observable<PedidoResponse[]> {
    return this.http.get<PedidoResponse[]>(`${this.apiUrl}/cliente/${encodeURIComponent(rut)}`);
  }

  crearPedido(pedido: CrearPedido): Observable<PedidoResponse> {
    return this.http.post<PedidoResponse>(this.apiUrl, pedido);
  }

  actualizarEstado(idPedido: number, estado: string): Observable<{ mensaje: string, estado: string, estadoNotificacion?: string }> {
    return this.http.patch<{ mensaje: string, estado: string, estadoNotificacion?: string }>(`${this.apiUrl}/${idPedido}/estado`, { estado });
  }

  obtenerPedidoPorId(id: number): Observable<PedidoResponse> {
    return this.http.get<PedidoResponse>(`${this.apiUrl}/${id}`);
  }

}
