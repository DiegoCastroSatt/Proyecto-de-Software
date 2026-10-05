import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

export interface Venta {
  idVenta: number;
  fecha: string;
  total: number;
  productos: { productoId: number | null; nombre: string; cantidad: number; precioUnitario: number; subtotal: number }[];
}

export interface VentaCreada {
  idVenta: number;
  fecha: string;
  total: number;
  productos: { productoId: number | null; cantidad: number; precioUnitario: number; subtotal: number }[];
}

export interface ProductoCaja {
  idProducto: number;
  codigoProducto: string;
  nombre: string;
  precio: number;
  stock: number;
  cantidad: number;
}

export interface ProductoVenta {
  codigoProducto: string;
  cantidad: number;
}

@Injectable({ providedIn: 'root' })
export class RegistroVentaService {
  private readonly http = inject(HttpClient);
  private readonly url = 'http://localhost:8080/api/Ventas';

  buscar(codigo: string): Observable<ProductoCaja> {
    return this.http.get<ProductoCaja>(`${this.url}/producto`, { params: { codigo } });
  }

  buscarCoincidencias(termino: string): Observable<ProductoCaja[]> {
    return this.http.get<{ id: number; codigo: string; nombre: string; precio: number; stock: number }[]>(
      'http://localhost:8080/api/Productos', { params: { termino } }
    ).pipe(map(productos => productos.map(p => ({
      idProducto: p.id, codigoProducto: p.codigo, nombre: p.nombre,
      precio: p.precio, stock: p.stock, cantidad: 1
    }))));
  }

  listar(): Observable<Venta[]> {
    return this.http.get<Venta[]>(this.url);
  }

  crear(productos: ProductoVenta[]): Observable<VentaCreada> {
    return this.http.post<VentaCreada>(this.url, { productos });
  }
}
