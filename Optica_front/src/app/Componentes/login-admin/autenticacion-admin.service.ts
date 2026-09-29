import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

export interface LoginAdministradorResponse {
  idAdministrador: number;
  nombre: string;
}

@Injectable({ providedIn: 'root' })
export class AutenticacionAdminService {
  readonly administrador = signal<LoginAdministradorResponse | null>(null);
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Autenticacion/admin';

  iniciarSesion(nombreUsuario: string, contrasena: string): Observable<LoginAdministradorResponse> {
    return this.http.post<LoginAdministradorResponse>(this.apiUrl, { nombreUsuario, contrasena }).pipe(tap(admin => this.administrador.set(admin)));
  }
  verificarSesion(): Observable<LoginAdministradorResponse> {
    return this.http.get<LoginAdministradorResponse>(`${this.apiUrl}/sesion`).pipe(tap(admin => this.administrador.set(admin)));
  }
  cerrarSesion(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/salir`, {}).pipe(tap(() => this.administrador.set(null)));
  }
}
