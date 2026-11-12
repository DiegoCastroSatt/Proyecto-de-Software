import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface AvisoReposicion {
  idAviso: number;
  idProducto: number;
  nombreProducto: string;
  fecha: string;
  estado: 'Abierta' | 'Cerrada';
}

@Injectable({ providedIn: 'root' })
export class ListaSolicitudesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/AvisosReposicion';

  obtenerMisSolicitudes(): Observable<AvisoReposicion[]> {
    return this.http.get<AvisoReposicion[]>(`${this.apiUrl}/mis-solicitudes`, {
      withCredentials: true
    });
  }
}
