import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CrearReserva {
  nombreCompleto: string;
  rut: string;
  telefono: string;
  correo: string;
  idHorario: number;
}

export interface HorarioDisponible {
  idHorario: number;
  fecha: string;
  hora: string;
}

export interface ReservaResponse {
  id: number;
  fecha: string;
  hora: string;
  estado: string;
}

@Injectable({ providedIn: 'root' })
export class ReservaService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Reservas';

  crearReserva(reserva: CrearReserva): Observable<ReservaResponse> {
    return this.http.post<ReservaResponse>(this.apiUrl, reserva);
  }

  obtenerDisponibles(): Observable<HorarioDisponible[]> {
    return this.http.get<HorarioDisponible[]>(`${this.apiUrl}/disponibles`);
  }
}