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

export interface SolicitarCambioCorreo {
  correo: string;
  contrasenaActual: string;
}

export interface RespuestaCambioCorreo { mensaje: string; }

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

  solicitarCambioCorreo(datos: SolicitarCambioCorreo): Observable<RespuestaCambioCorreo> {
    return this.http.post<RespuestaCambioCorreo>(
      'http://localhost:8080/api/Autenticacion/cliente/correo/cambio', datos, { withCredentials: true });
  }

  // Elimina la cuenta web y anonimiza la ficha; la contraseña actual confirma la identidad.
  eliminarCuenta(contrasenaActual: string): Observable<RespuestaCambioCorreo> {
    return this.http.delete<RespuestaCambioCorreo>(
      'http://localhost:8080/api/Autenticacion/cliente/cuenta',
      { body: { contrasenaActual }, withCredentials: true });
  }
}
