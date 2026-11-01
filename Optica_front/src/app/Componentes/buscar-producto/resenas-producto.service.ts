import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { obtenerOrigenApi } from '../../shared/services/api-origin';

export interface ResenaProducto {
  id: number;
  idProducto: number;
  calificacion: number;
  comentario: string;
  nombreCliente: string;
  fechaCreacionUtc: string;
  fechaActualizacionUtc?: string | null;
}

export interface ResumenResenas {
  promedio: number;
  cantidad: number;
}

export interface ResenasProductoResponse {
  resumen: ResumenResenas;
  resenas: ResenaProducto[];
}

export interface ProductoResenable {
  idProducto: number;
  yaResenado: boolean;
}

@Injectable({ providedIn: 'root' })
export class ResenasProductoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${obtenerOrigenApi()}/api/Resenas`;

  obtenerPorProducto(idProducto: number): Observable<ResenasProductoResponse> {
    return this.http.get<ResenasProductoResponse>(`${this.apiUrl}/producto/${idProducto}`);
  }

  obtenerMisProductos(): Observable<ProductoResenable[]> {
    return this.http.get<ProductoResenable[]>(`${this.apiUrl}/mis-productos`, { withCredentials: true });
  }

  publicar(idProducto: number, calificacion: number, comentario: string): Observable<ResenaProducto> {
    return this.http.post<ResenaProducto>(`${this.apiUrl}/producto/${idProducto}`, { calificacion, comentario }, { withCredentials: true });
  }
}
