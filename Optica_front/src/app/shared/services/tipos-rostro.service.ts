import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { obtenerOrigenApi } from './api-origin';

export interface TipoRostro {
  id: number; codigo: string; nombre: string; rasgos: string; consejo: string;
  activo: boolean; rutaImagen: string | null; marcos: number[];
}
export function imagenRostro(rostro: TipoRostro): string {
  return rostro.rutaImagen ?? `${obtenerOrigenApi()}/api/Rostros/${rostro.id}/imagen`;
}
@Injectable({ providedIn: 'root' })
export class TiposRostroService {
  private readonly http = inject(HttpClient);
  private readonly url = 'http://localhost:8080/api/Rostros';
  listar() { return this.http.get<TipoRostro[]>(this.url); }
  activos() { return this.http.get<TipoRostro[]>(`${this.url}/activos`); }
  crear(datos: { nombre: string; rasgos: string; consejo: string; imagen: File; marcos: number[] }) {
    const form = new FormData();
    form.append('nombre', datos.nombre); form.append('rasgos', datos.rasgos); form.append('consejo', datos.consejo);
    form.append('imagen', datos.imagen, datos.imagen.name);
    datos.marcos.forEach(id => form.append('marcos', String(id)));
    return this.http.post<TipoRostro>(this.url, form);
  }
  cambiarEstado(id: number, activo: boolean) { return this.http.patch<void>(`${this.url}/${id}/estado`, { activo }); }
  asociarMarcos(id: number, marcos: number[]) { return this.http.put<void>(`${this.url}/${id}/marcos`, { marcos }); }
  borrar(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
}
