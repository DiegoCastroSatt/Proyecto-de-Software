import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface AvisoReposicionAdmin {
  idAviso: number;
  rutCliente: string;
  nombreCliente: string;
  correoCliente: string | null;
  idProducto: number;
  nombreProducto: string;
  fecha: string;
  estado: 'Abierta' | 'Cerrada';
}

@Injectable({ providedIn: 'root' })
export class SolicitudesAdminService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/AvisosReposicion';

  obtenerTodas(): Observable<AvisoReposicionAdmin[]> {
    return this.http.get<AvisoReposicionAdmin[]>(this.apiUrl, { withCredentials: true });
  }
}
