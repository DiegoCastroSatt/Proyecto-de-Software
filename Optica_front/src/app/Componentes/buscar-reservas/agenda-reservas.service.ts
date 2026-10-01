import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface ReservaAgenda {
  id: number;
  fecha: string;
  horaInicio: string;
  horaFin: string;
  estado: string;
  motivo: string | null;
  nombreCliente: string;
  rutCliente: string;
  telefonoCliente: string | null;
  correoCliente: string | null;
}

@Injectable({ providedIn: 'root' })
export class AgendaReservasService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Reservas/agenda';

  listar(historialAtendidas = false): Observable<ReservaAgenda[]> {
    const params = new HttpParams().set('historialAtendidas', historialAtendidas);
    return this.http.get<ReservaAgenda[]>(this.apiUrl, { params });
  }
}
