import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { AutenticacionClienteService } from '../../shared/services/autenticacion-cliente.service';

@Component({
  selector: 'app-confirmar-cuenta-cliente',
  standalone: true,
  templateUrl: './confirmar-cuenta-cliente.html',
  styleUrl: './confirmar-cuenta-cliente.css',
})
export class ConfirmarCuentaClienteComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly autenticacion = inject(AutenticacionClienteService);

  readonly estado = signal<'cargando' | 'exito' | 'error'>('cargando');
  readonly mensaje = signal('');

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token?.trim()) {
      this.estado.set('error');
      this.mensaje.set('El enlace no contiene un token de confirmación. Solicita un nuevo correo.');
      return;
    }

    this.autenticacion.confirmarCuenta(token.trim()).subscribe({
      next: (respuesta) => {
        this.estado.set('exito');
        this.mensaje.set(respuesta.mensaje);
      },
      error: (error: HttpErrorResponse) => {
        this.estado.set('error');
        this.mensaje.set(typeof error.error?.mensaje === 'string'
          ? error.error.mensaje
          : 'No se pudo validar el enlace. Solicita un nuevo correo de confirmación.');
      }
    });
  }

  irAlLogin(): void {
    void this.router.navigateByUrl('/cliente/login');
  }
}
