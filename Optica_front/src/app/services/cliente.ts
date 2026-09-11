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
}