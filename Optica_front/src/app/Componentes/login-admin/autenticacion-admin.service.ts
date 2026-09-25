import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface LoginAdministradorResponse {
  idAdministrador: number;
  nombre: string;
}

@Injectable({ providedIn: 'root' })
export class AutenticacionAdminService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Autenticacion/admin';

  iniciarSesion(nombreUsuario: string, contrasena: string): Observable<LoginAdministradorResponse> {
    return this.http.post<LoginAdministradorResponse>(this.apiUrl, { nombreUsuario, contrasena });
  }
}