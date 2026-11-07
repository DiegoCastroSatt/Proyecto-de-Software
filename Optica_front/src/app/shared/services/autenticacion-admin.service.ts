import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal, computed } from '@angular/core';
import { Observable, tap } from 'rxjs';

export interface LoginAdministradorResponse {
  idAdministrador: number;
  nombre: string;
  tipoUsuario?: 'Administrador' | 'Trabajador';
  idTrabajador?: number;
  rol?: string;
  permisos?: string[];
}

@Injectable({ providedIn: 'root' })
export class AutenticacionAdminService {
  readonly administrador = signal<LoginAdministradorResponse | null>(null);
  readonly esAdministrador = computed(() => {
    const usuario = this.administrador();
    return (
      !!usuario &&
      (usuario.tipoUsuario === 'Administrador' ||
        (!usuario.tipoUsuario && usuario.idAdministrador > 0))
    );
  });
  puede(permiso: string): boolean {
    const usuario = this.administrador();
    return (
      this.esAdministrador() ||
      (usuario?.tipoUsuario === 'Trabajador' && !!usuario.permisos?.includes(permiso))
    );
  }
  esPersonal(): boolean {
    const usuario = this.administrador();
    return (
      this.esAdministrador() ||
      (usuario?.tipoUsuario === 'Trabajador' &&
        Number.isInteger(usuario.idTrabajador) &&
        usuario.idTrabajador! > 0)
    );
  }
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Autenticacion/admin';

  verificarSesion(): Observable<LoginAdministradorResponse> {
    return this.http
      .get<LoginAdministradorResponse>(`${this.apiUrl}/sesion`)
      .pipe(tap((admin) => this.administrador.set(admin)));
  }
  cambiarContrasena(datos: {
    contrasenaActual: string;
    nuevaContrasena: string;
    confirmarContrasena: string;
  }): Observable<{ mensaje: string }> {
    return this.http.post<{ mensaje: string }>(
      'http://localhost:8080/api/CuentaTrabajador/contrasena',
      datos,
    );
  }
  cerrarSesion(): Observable<void> {
    return this.http
      .post<void>(`${this.apiUrl}/salir`, {})
      .pipe(tap(() => this.administrador.set(null)));
  }
}
