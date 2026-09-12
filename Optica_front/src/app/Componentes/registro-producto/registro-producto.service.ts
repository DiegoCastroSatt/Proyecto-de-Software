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

export interface CatalogoItem {
  tipo: string;
  nombre: string;
}

@Injectable({ providedIn: 'root' })
export class RegistrarProductoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Productos';
  private readonly catalogosUrl = 'http://localhost:8080/api/Catalogos';

  crearProducto(producto: CrearProducto): Observable<ProductoResponse> {
    return this.http.post<ProductoResponse>(this.apiUrl, producto);
  }

  obtenerCatalogo(tipo: string): Observable<CatalogoItem[]> {
    return this.http.get<CatalogoItem[]>(`${this.catalogosUrl}?tipo=${encodeURIComponent(tipo)}`);
  }

  crearCatalogoItem(tipo: string, nombre: string): Observable<CatalogoItem> {
    return this.http.post<CatalogoItem>(this.catalogosUrl, { tipo, nombre });
  }
}
