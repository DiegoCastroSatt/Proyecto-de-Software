import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Graduacion {
  ojo: 'OD' | 'OI';
  esfera: number;
  cilindro: number;
  eje: number;
  adicion: number;
}

export interface CrearReceta {
  rut: string;
  fecha: string;
  observaciones?: string;
  graduaciones?: Graduacion[];
  imagen?: File;
}

export interface RecetaResponse {
  rut: string;
  id: number;
  clienteId: number;
  fecha: string;
  observaciones?: string;
  imagenUrl?: string;
  graduaciones: Graduacion[];
}

@Injectable({ providedIn: 'root' })
export class RegistrarRecetaService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Recetas';

  crearReceta(receta: CrearReceta): Observable<RecetaResponse> {
    return this.http.post<RecetaResponse>(this.apiUrl, this.formData(receta));
  }

  obtenerReceta(id: number): Observable<RecetaResponse> {
    return this.http.get<RecetaResponse>(`${this.apiUrl}/${id}`);
  }

  actualizarReceta(id: number, receta: CrearReceta): Observable<RecetaResponse> {
    return this.http.put<RecetaResponse>(`${this.apiUrl}/${id}`, this.formData(receta));
  }

  private formData(receta: CrearReceta): FormData {
    const formData = new FormData();
    formData.append('rut', receta.rut);
    formData.append('fecha', receta.fecha);

    if (receta.observaciones) {
      formData.append('observaciones', receta.observaciones);
    }

    if (receta.graduaciones && receta.graduaciones.length > 0) {
      formData.append('graduacionesJson', JSON.stringify(receta.graduaciones));
    }

    if (receta.imagen) {
      formData.append('imagen', receta.imagen, receta.imagen.name);
    }

    return formData;
  }
}