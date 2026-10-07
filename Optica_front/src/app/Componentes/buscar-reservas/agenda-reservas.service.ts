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

export interface HorarioDisponible {
  idHorario: number;
  fecha: string;
  hora: string;
  horaFin: string;
}

@Injectable({ providedIn: 'root' })
export class AgendaReservasService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Reservas/agenda';

  listar(historialAtendidas = false): Observable<ReservaAgenda[]> {
    const params = new HttpParams().set('historialAtendidas', historialAtendidas);
    return this.http.get<ReservaAgenda[]>(this.apiUrl, { params });
  }

  disponibles(fecha: string, excluirReservaId: number): Observable<HorarioDisponible[]> {
    const params = new HttpParams().set('fecha', fecha).set('excluirReservaId', excluirReservaId);
    return this.http.get<HorarioDisponible[]>('http://localhost:8080/api/Reservas/disponibles', { params });
  }

  reprogramar(id: number, idHorario: number): Observable<void> {
    return this.http.put<void>(`http://localhost:8080/api/Reservas/${id}/reprogramar`, { idHorario });
  }

  cancelar(id: number): Observable<void> {
    return this.http.post<void>(`http://localhost:8080/api/Reservas/${id}/cancelar`, {});
  }
}
