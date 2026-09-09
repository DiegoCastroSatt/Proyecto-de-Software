import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CrearProducto {
  codigo: string;
  nombre: string;
  marca: string;
  modelo: string;
  color: string;
  categoria: string;
  precio: number;
  stock: number;
  stockMinimo: number;
  estado: string;
}

export interface ProductoResponse {
  id: number;
  codigo: string;
  nombre: string;
  estado: string;
}

@Injectable({ providedIn: 'root' })
export class RegistrarProductoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Productos';

  crearProducto(producto: CrearProducto): Observable<ProductoResponse> {
    return this.http.post<ProductoResponse>(this.apiUrl, producto);
  }
}
