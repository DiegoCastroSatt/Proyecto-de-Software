import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Venta {
  idVenta: number;
  fecha: string;
  total: number;
  productos: { productoId: number; nombre: string; cantidad: number; precioUnitario: number; subtotal: number }[];
}

export interface VentaCreada {
  idVenta: number;
  fecha: string;
  total: number;
  producto: string;
  cantidad: number;
}

@Injectable({ providedIn: 'root' })
export class RegistroVentaService {
  private readonly http = inject(HttpClient);
  private readonly url = 'http://localhost:8080/api/Ventas';

  listar(): Observable<Venta[]> {
    return this.http.get<Venta[]>(this.url);
  }

  crear(codigoProducto: string, cantidad: number): Observable<VentaCreada> {
    return this.http.post<VentaCreada>(this.url, { codigoProducto, cantidad });
  }
}
