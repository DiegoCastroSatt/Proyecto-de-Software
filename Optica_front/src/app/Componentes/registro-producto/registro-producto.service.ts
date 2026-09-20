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
  imagen?: File;
}

export interface ProductoResponse {
  id: number;
  codigo: string;
  nombre: string;
  estado: string;
  rutaImagen?: string;
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
    const formData = new FormData();
    formData.append('codigo', producto.codigo);
    formData.append('nombre', producto.nombre);
    formData.append('marca', producto.marca);
    formData.append('modelo', producto.modelo);
    formData.append('color', producto.color);
    formData.append('categoria', producto.categoria);
    formData.append('precio', producto.precio.toString());
    formData.append('stock', producto.stock.toString());
    formData.append('stockMinimo', producto.stockMinimo.toString());
    formData.append('estado', producto.estado);
    if (producto.imagen) {
      formData.append('imagen', producto.imagen, producto.imagen.name);
    }

    return this.http.post<ProductoResponse>(this.apiUrl, formData);
  }

  obtenerCatalogo(tipo: string): Observable<CatalogoItem[]> {
    return this.http.get<CatalogoItem[]>(`${this.catalogosUrl}?tipo=${encodeURIComponent(tipo)}`);
  }

  crearCatalogoItem(tipo: string, nombre: string): Observable<CatalogoItem> {
    return this.http.post<CatalogoItem>(this.catalogosUrl, { tipo, nombre });
  }
}
