import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
export interface Permiso {
  codigo: string;
  nombre: string;
}
export interface Rol {
  idRol: number;
  nombre: string;
  permisos: string[];
}
export interface Trabajador {
  idTrabajador: number;
  nombre: string;
  nombreUsuario: string;
  correo: string;
  estado: 'Activo' | 'Inactivo';
  idRol: number;
  rol: string;
  tipoUsuario: 'Trabajador';
}
@Injectable({ providedIn: 'root' })
export class AccesosService {
  private readonly http = inject(HttpClient);
  private readonly url = 'http://localhost:8080/api/Accesos';
  permisos() {
    return this.http.get<Permiso[]>(`${this.url}/permisos`);
  }
  roles() {
    return this.http.get<Rol[]>(`${this.url}/roles`);
  }
  trabajadores() {
    return this.http.get<Trabajador[]>(`${this.url}/trabajadores`);
  }
  crearTrabajador(datos: { nombre: string; nombreUsuario: string; correo: string; contrasena: string; idRol: number }) {
    return this.http.post(`${this.url}/trabajadores`, datos);
  }
  guardarRol(id: number | null, datos: { nombre: string; permisos: string[] }) {
    return id === null
      ? this.http.post<Rol>(`${this.url}/roles`, datos)
      : this.http.put<Rol>(`${this.url}/roles/${id}`, datos);
  }
  asignarAcceso(t: Trabajador) {
    return this.http.put<void>(`${this.url}/trabajadores/${t.idTrabajador}/acceso`, {
      idRol: +t.idRol,
      estado: t.estado,
    });
  }
}
