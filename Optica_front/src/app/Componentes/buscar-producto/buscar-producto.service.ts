import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Producto {
  id: number;
  idTipoMarco?: number | null;
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
  tieneVentas: boolean;
}

export interface HistorialStockMovimiento {
  idCambio: number;
  idProducto: number;
  motivoMovimiento: string;
  idResponsable: number;
  nombreResponsable: string;
  tipoStock: 'Normal' | 'Minimo';
  valorAnterior: number;
  valorActual: number;
  fechaCambio: string;
}

export interface HistorialStockPaginado {
  pagina: number;
  tamanoPagina: number;
  totalRegistros: number;
  totalPaginas: number;
  movimientos: HistorialStockMovimiento[];
}

export type ProductoEditable = Omit<Producto, 'id' | 'rutaImagen' | 'tieneVentas'> & { imagen?: File };

export interface CatalogoItem {
  tipo: string;
  nombre: string;
}

export interface AvisoReposicion {
  idAviso: number;
  idProducto: number;
  nombreProducto: string;
  fecha: string;
  estado: string;
}

@Injectable({ providedIn: 'root' })
export class BuscarProductoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Productos';
  private readonly avisosReposicionUrl = 'http://localhost:8080/api/AvisosReposicion';

  buscar(termino: string): Observable<Producto[]> {
    return this.http.get<Producto[]>(this.apiUrl, { params: { termino } });
  }

  buscarPagina(params: Record<string, string | number | boolean>): Observable<{
    productos: Producto[]; haySiguiente: boolean; totalPaginas: number; categorias?: string[]; colores?: string[]; marcas?: string[];
  }> {
    return this.http.get<{
      productos: Producto[]; haySiguiente: boolean; totalPaginas: number; categorias?: string[]; colores?: string[]; marcas?: string[];
    }>(`${this.apiUrl}/pagina`, { params });
  }

  solicitarAvisoReposicion(idProducto: number): Observable<AvisoReposicion> {
    return this.http.post<AvisoReposicion>(
      `${this.avisosReposicionUrl}/producto/${idProducto}`,
      {},
      { withCredentials: true }
    );
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

  actualizarStock(id: number, stock: number, stockMinimo: number, motivoCambio: string): Observable<Producto> {
    return this.http.patch<Producto>(`${this.apiUrl}/${id}/stock`, { stock, stockMinimo, motivoCambio });
  }

  consultarHistorialStock(
    idProducto: number,
    pagina: number,
    tamanoPagina = 20
  ): Observable<HistorialStockPaginado> {
    return this.http.get<HistorialStockPaginado>(`${this.apiUrl}/${idProducto}/historial-stock`, {
      params: { pagina, tamanoPagina }
    });
  }

  consultarHistorialStockReciente(pagina: number, tamanoPagina = 20): Observable<HistorialStockPaginado> {
    return this.http.get<HistorialStockPaginado>(`${this.apiUrl}/historial-stock`, {
      params: { pagina, tamanoPagina }
    });
  }

  obtenerCatalogo(tipo: string): Observable<CatalogoItem[]> {
    return this.http.get<CatalogoItem[]>(`${this.catalogosUrl}?tipo=${encodeURIComponent(tipo)}`);
  }

  crearCatalogoItem(tipo: string, nombre: string): Observable<CatalogoItem> {
    return this.http.post<CatalogoItem>(this.catalogosUrl, { tipo, nombre });
  }

  eliminar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  private readonly catalogosUrl = 'http://localhost:8080/api/Catalogos';
}
