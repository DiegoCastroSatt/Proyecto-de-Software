import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface GraduacionHistorial {
  ojo: 'OD' | 'OI';
  esfera: number | null;
  cilindro: number | null;
  eje: number | null;
  adicion: number | null;
}

export interface RecetaHistorial {
  id: number;
  fecha: string;
  observaciones?: string;
  graduaciones: GraduacionHistorial[];
}

@Injectable({ providedIn: 'root' })
export class HistorialGraduacionesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Recetas/historial';

  obtenerHistorial(rut: string): Observable<RecetaHistorial[]> {
    return this.http.get<RecetaHistorial[]>(this.apiUrl, {
      params: { rut },
      withCredentials: true,
    });
  }
}