import { FormaLenteDiseno } from '../../Componentes/diseno/diseno-marco';
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { obtenerOrigenApi } from './api-origin';

export interface TipoMarco {
  id: number;
  codigo: string;
  nombre: string;
  categoria: string;
  activo: boolean;
  rostros: string[];
  stock: number;
  productosVinculados?: number;
}
export interface ProductoVinculableMarco {
  id: number; codigo: string; nombre: string; categoria: string; marca: string;
  stock: number; estado: string; idTipoMarco: number | null;
}
export function imagenMarco(id: number): string { return `${obtenerOrigenApi()}/api/Marcos/${id}/imagen`; }

@Injectable({ providedIn: 'root' })
export class TiposMarcoService {
  private readonly http = inject(HttpClient);
  private readonly url = 'http://localhost:8080/api/Marcos';
  listarFormas() { return this.http.get<FormaLenteDiseno[]>(`${this.url}/formas-lente`); }
  listar() { return this.http.get<TipoMarco[]>(this.url); }
  disponibles() { return this.http.get<TipoMarco[]>(`${this.url}/disponibles`); }
  obtenerDiseno(id: number) { return this.http.get(`${this.url}/${id}/imagen`, { responseType: 'text' }); }
  validarDiseno(svg: string) { return this.http.post<{ svg: string }>(`${this.url}/validar-diseno`, { svg }); }
  crear(datos: { nombre: string; categoria: string; svg: string; rostros: string[] }) {
    return this.http.post<TipoMarco>(this.url, datos);
  }
  cambiarEstado(id: number, activo: boolean) { return this.http.patch<void>(`${this.url}/${id}/estado`, { activo }); }
  productosVinculables() { return this.http.get<ProductoVinculableMarco[]>(`${this.url}/productos-vinculables`); }
  vincularProductos(id: number, productos: number[]) {
    return this.http.put<TipoMarco[]>(`${this.url}/${id}/productos`, { productos });
  }
  borrar(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
}
