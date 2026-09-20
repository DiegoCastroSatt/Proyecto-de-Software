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

    return this.http.post<RecetaResponse>(this.apiUrl, formData);
  }
}