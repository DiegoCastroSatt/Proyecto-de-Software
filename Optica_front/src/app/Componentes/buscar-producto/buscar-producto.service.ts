import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Producto {
  id: number;
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
  rutaImagen?: string;
}

export type ProductoEditable = Omit<Producto, 'id' | 'rutaImagen'> & { imagen?: File };

@Injectable({ providedIn: 'root' })
export class BuscarProductoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Productos';

  buscar(termino: string): Observable<Producto[]> {
    return this.http.get<Producto[]>(this.apiUrl, { params: { termino } });
  }

  actualizar(id: number, producto: ProductoEditable): Observable<Producto> {
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

    return this.http.put<Producto>(`${this.apiUrl}/${id}`, formData);
  }
}
