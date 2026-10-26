import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AutenticacionClienteService } from '../login-cliente/autenticacion-cliente.service';

@Component({
  selector: 'app-restablecer-contrasena',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './restablecer-contrasena.html',
  styleUrl: './restablecer-contrasena.css',
})
export class RestablecerContrasenaComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly autenticacion = inject(AutenticacionClienteService);

  token = signal('');
  nuevaContrasena = '';
  confirmarContrasena = '';

  cargando = signal(false);
  exito = signal(false);
  error = signal('');
  tokenInvalido = signal(false);

  ngOnInit(): void {
    const tokenParam = this.route.snapshot.queryParamMap.get('token');
    if (!tokenParam || !tokenParam.trim()) {
      this.tokenInvalido.set(true);
      this.error.set('El enlace de recuperación no es válido o no incluye un token de seguridad.');
    } else {
      this.token.set(tokenParam.trim());
    }
  }

  restablecer(): void {
    if (this.cargando() || this.tokenInvalido()) return;
    this.error.set('');

    if (!this.nuevaContrasena || !this.confirmarContrasena) {
      this.error.set('Completa ambos campos de contraseña.');
      return;
    }

    if (!this.contrasenaValida(this.nuevaContrasena)) {
      this.error.set('La contraseña debe tener al menos 8 caracteres e incluir letras, números y un símbolo.');
      return;
    }

    if (this.nuevaContrasena !== this.confirmarContrasena) {
      this.error.set('Las contraseñas no coinciden.');
      return;
    }

    this.cargando.set(true);
    this.autenticacion.confirmarRecuperacion(this.token(), this.nuevaContrasena, this.confirmarContrasena).subscribe({
      next: () => {
        this.cargando.set(false);
        this.exito.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.cargando.set(false);
        if (typeof err.error?.mensaje === 'string') {
          this.error.set(err.error.mensaje);
        } else {
          this.error.set('No se pudo restablecer la contraseña. Es posible que el enlace haya expirado.');
        }
      }
    });
  }

  irAlLogin(): void {
    void this.router.navigateByUrl('/cliente/login');
  }

  private contrasenaValida(valor: string): boolean {
    return valor.length >= 8 && valor.length <= 255
      && /\p{L}/u.test(valor) && /\p{N}/u.test(valor) && /[^\p{L}\p{N}\s]/u.test(valor);
  }
}