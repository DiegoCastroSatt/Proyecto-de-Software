import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { AutenticacionAdminService, LoginAdministradorResponse } from './autenticacion-admin.service';
import { AutenticacionClienteService, ClienteSesion } from './autenticacion-cliente.service';

export type ResultadoInicioSesion =
  | { tipoUsuario: 'Cliente'; cliente: ClienteSesion; personal: null }
  | { tipoUsuario: 'Administrador' | 'Trabajador'; personal: LoginAdministradorResponse; cliente: null };

@Injectable({ providedIn: 'root' })
export class AutenticacionService {
  private readonly http = inject(HttpClient);
  private readonly personal = inject(AutenticacionAdminService);
  private readonly clientes = inject(AutenticacionClienteService);
  private readonly apiUrl = 'http://localhost:8080/api/Autenticacion';

  iniciarSesion(identificador: string, contrasena: string) {
    return this.http.post<ResultadoInicioSesion>(`${this.apiUrl}/login`,
      { correo: identificador, contrasena }, { withCredentials: true }).pipe(tap(resultado => {
        this.personal.administrador.set(resultado.personal);
        this.clientes.cliente.set(resultado.cliente);
      }));
  }

  solicitarRecuperacion(identificador: string) {
    return this.http.post<{ mensaje: string }>(`${this.apiUrl}/recuperacion/solicitar`,
      { identificador }, { withCredentials: true });
  }

  confirmarRecuperacion(datos: { identificador: string; codigo: string; nuevaContrasena: string; confirmarContrasena: string }) {
    return this.http.post<{ mensaje: string }>(`${this.apiUrl}/recuperacion/confirmar`, datos, { withCredentials: true });
  }
}
