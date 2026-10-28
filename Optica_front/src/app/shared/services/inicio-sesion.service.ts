import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { AutenticacionAdminService, LoginAdministradorResponse } from '../../Componentes/login-admin/autenticacion-admin.service';
import { AutenticacionClienteService, ClienteSesion } from '../../Componentes/login-cliente/autenticacion-cliente.service';

export type ResultadoInicioSesion =
  | { tipoUsuario: 'Cliente'; cliente: ClienteSesion; personal: null }
  | { tipoUsuario: 'Administrador' | 'Trabajador'; personal: LoginAdministradorResponse; cliente: null };

@Injectable({ providedIn: 'root' })
export class InicioSesionService {
  private readonly http = inject(HttpClient);
  private readonly personal = inject(AutenticacionAdminService);
  private readonly clientes = inject(AutenticacionClienteService);

  iniciarSesion(correo: string, contrasena: string) {
    return this.http.post<ResultadoInicioSesion>('http://localhost:8080/api/Autenticacion/login',
      { correo, contrasena }, { withCredentials: true }).pipe(tap(resultado => {
        this.personal.administrador.set(resultado.personal);
        this.clientes.cliente.set(resultado.cliente);
      }));
  }
}
