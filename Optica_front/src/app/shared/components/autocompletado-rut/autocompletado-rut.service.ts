import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ClienteSugerencia {
  rut: string;
  nombre: string;
  apellido: string;
}

@Injectable({ providedIn: 'root' })
export class AutocompletadoRutService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Recetas/clientes/sugerencias';

  buscarSugerencias(termino: string): Observable<ClienteSugerencia[]> {
    return this.http.get<ClienteSugerencia[]>(this.apiUrl, {
      params: { termino },
      withCredentials: true,
    });
  }
}