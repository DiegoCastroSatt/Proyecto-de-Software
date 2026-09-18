import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export type EstadoHorario = 'Habilitada' | 'Inhabilitada';

export interface Horario {
  idHorario: number;
  fecha: string;
  horaInicio: string;
  horaFin: string;
  estado: EstadoHorario;
}

export interface CrearHorariosRequest {
  fecha: string;
  horaInicio: string;
  horaFin: string;
  duracionMinutos: number;
}

@Injectable({ providedIn: 'root' })
export class GestionHorariosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Horarios';

  listar(): Observable<Horario[]> {
    return this.http.get<Horario[]>(this.apiUrl);
  }

  crearBloques(request: CrearHorariosRequest): Observable<Horario[]> {
    return this.http.post<Horario[]>(this.apiUrl, request);
  }

  cambiarEstado(idHorario: number, estado: EstadoHorario): Observable<Horario> {
    return this.http.patch<Horario>(`${this.apiUrl}/${idHorario}/estado`, { estado });
  }
}
