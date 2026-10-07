import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Cliente {
  idCliente?: number;
  rut: string;
  nombre: string;
  apellido: string;
  telefono?: string;
  correo?: string;
  estado?: string;
  fechaRegistro?: string;
}

export interface HistorialGraduacion {
  ojo: string;
  esfera?: number;
  cilindro?: number;
  eje?: number;
  adicion?: number;
}

export interface HistorialReceta {
  idReceta: number;
  fecha: string;
  observaciones?: string;
  imagenPath?: string;
  graduaciones: HistorialGraduacion[];
}

export interface HistorialPedido {
  idPedido: number;
  idReceta?: number;
  fecha: string;
  estado: string;
  total: number;
  anotaciones?: string;
}

export interface HistorialCliente {
  idCliente: number;
  rut: string;
  nombreCompleto: string;
  telefono?: string;
  correo?: string;
  estado: string;
  recetas: HistorialReceta[];
  pedidos: HistorialPedido[];
}

@Injectable({
  providedIn: 'root'
})
export class ClienteService {
  private apiUrl = 'http://localhost:8080/api/Clientes';

  constructor(private http: HttpClient) {}

  registrar(cliente: Cliente): Observable<Cliente> {
    return this.http.post<Cliente>(this.apiUrl, cliente);
  }

  buscar(termino: string = ''): Observable<Cliente[]> {
    let params = new HttpParams();
    if (termino.trim()) {
      params = params.set('termino', termino.trim());
    }
    return this.http.get<Cliente[]>(`${this.apiUrl}/buscar`, { params });
  }

  // Método para actualizar la información del cliente
  actualizar(id: number, cliente: Partial<Cliente>): Observable<Cliente> {
    return this.http.put<Cliente>(`${this.apiUrl}/${id}`, cliente);
  }

  cambiarEstado(id: number, nuevoEstado: 'Activo' | 'Inactivo'): Observable<any> {
    return this.http.patch<any>(`${this.apiUrl}/${id}/estado`, { nuevoEstado });
  }

  obtenerHistorial(id: number): Observable<HistorialCliente> {
    return this.http.get<HistorialCliente>(`${this.apiUrl}/${id}/historial`);
  }
}