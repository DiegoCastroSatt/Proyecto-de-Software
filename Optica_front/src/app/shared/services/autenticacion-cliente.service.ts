import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

export interface ClienteSesion {
  idCliente: number;
  nombre: string;
  correo: string;
}

export interface RegistroCliente {
  nombre: string;
  apellido: string;
  rut: string;
  correo: string;
  telefono: string;
  contrasena: string;
  confirmarContrasena: string;
}

export interface ResultadoRegistroCliente {
  mensaje: string;
  correoConfirmacionEnviado: boolean;
}

export interface RespuestaAutenticacion { mensaje: string; }

@Injectable({ providedIn: 'root' })
export class AutenticacionClienteService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:8080/api/Autenticacion/cliente';
  readonly cliente = signal<ClienteSesion | null>(null);

  registrar(datos: RegistroCliente): Observable<ResultadoRegistroCliente> {
    return this.http.post<ResultadoRegistroCliente>(`${this.apiUrl}/registro`, datos, { withCredentials: true });
  }

  confirmarCuenta(token: string): Observable<RespuestaAutenticacion> {
    return this.http.post<RespuestaAutenticacion>(`${this.apiUrl}/registro/confirmar`, { token });
  }

  reenviarConfirmacion(correo: string): Observable<RespuestaAutenticacion> {
    return this.http.post<RespuestaAutenticacion>(`${this.apiUrl}/registro/confirmacion/reenviar`, { correo });
  }

  verificarSesion(): Observable<ClienteSesion> {
    return this.http.get<ClienteSesion>(`${this.apiUrl}/sesion`, { withCredentials: true })
      .pipe(tap(cliente => this.cliente.set(cliente)));
  }

  cerrarSesion(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/salir`, {}, { withCredentials: true })
      .pipe(tap(() => this.cliente.set(null)));
  }
}
