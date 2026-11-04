import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface PerfilCliente {
  rut: string;
  nombre: string;
  apellido: string;
  telefono: string | null;
  correo: string | null;
  estado: string;
}

export interface ActualizarPerfilCliente {
  nombre: string;
  apellido: string;
  telefono: string | null;
}

@Injectable({ providedIn: 'root' })
export class PerfilService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Clientes/perfil';

  obtener(): Observable<PerfilCliente> {
    return this.http.get<PerfilCliente>(this.apiUrl, { withCredentials: true });
  }

  actualizar(datos: ActualizarPerfilCliente): Observable<PerfilCliente> {
    return this.http.put<PerfilCliente>(this.apiUrl, datos, { withCredentials: true });
  }
}
