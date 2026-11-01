import { Component, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AutenticacionAdminService } from './autenticacion-admin.service';

@Component({
  selector: 'app-login-admin',
  imports: [FormsModule],
  templateUrl: './login-admin.html',
  styleUrl: './login-admin.css',
})
export class LoginAdmin {

  usuario = '';
  contrasena = '';
  error = signal('');
  exito = signal('');
  nombreUsuarioRecuperacion = '';
  codigoRecuperacion = '';
  nuevaContrasena = '';
  confirmarContrasena = '';
  mensajeRecuperacion = signal('');
  errorRecuperacion = signal('');
  procesandoRecuperacion = signal(false);
  mostrarSolicitudRecuperacion = signal(false);
  mostrarConfirmacionRecuperacion = signal(false);
  private secuenciaRecuperacion = 0;
  
  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private autenticacionAdmin: AutenticacionAdminService
  ) {
    if (this.route.snapshot.queryParamMap.get('claveActualizada') === '1')
      this.exito.set('Contraseña actualizada. Inicia sesión con tu nueva contraseña.');
    if (this.route.snapshot.queryParamMap.get('recuperar') === '1') this.abrirRecuperacion();
  }

  onLogin(): void{
    this.error.set('');
    this.exito.set('');

    this.autenticacionAdmin.iniciarSesion(this.usuario.trim(), this.contrasena).subscribe({
      next: (resultado) => {
        this.contrasena = '';
        sessionStorage.removeItem('isAdmin');

        const destino = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/admin/panel';
        void this.router.navigateByUrl(destino === '/admin/panel' || destino.startsWith('/admin/panel/') ? destino : '/admin/panel');
      },
      error: (error: HttpErrorResponse) => {
        this.error.set(error.status === 401
          ? 'Nombre de usuario o contraseña incorrectos.'
          : 'No se pudo conectar con el servidor. Intenta nuevamente.');
      }
    });

  }

  abrirRecuperacion(): void {
    this.secuenciaRecuperacion++;
    this.errorRecuperacion.set('');
    this.mensajeRecuperacion.set('');
    this.nombreUsuarioRecuperacion = this.usuario.trim();
    this.codigoRecuperacion = '';
    this.nuevaContrasena = '';
    this.confirmarContrasena = '';
    this.mostrarSolicitudRecuperacion.set(true);
    this.mostrarConfirmacionRecuperacion.set(false);
  }

  solicitarCodigo(): void {
    if (this.procesandoRecuperacion()) return;
    const nombreUsuario = this.nombreUsuarioRecuperacion.trim();
    if (!nombreUsuario) {
      this.errorRecuperacion.set('Ingresa el nombre de usuario del personal.');
      return;
    }

    const secuencia = this.secuenciaRecuperacion;
    this.procesandoRecuperacion.set(true);
    this.errorRecuperacion.set('');
    this.autenticacionAdmin.solicitarRecuperacion(nombreUsuario).subscribe({
      next: (resultado) => {
        if (secuencia !== this.secuenciaRecuperacion) return;
        this.procesandoRecuperacion.set(false);
        this.mensajeRecuperacion.set(resultado.mensaje);
        this.mostrarSolicitudRecuperacion.set(false);
        this.mostrarConfirmacionRecuperacion.set(true);
      },
      error: (error: HttpErrorResponse) => {
        if (secuencia !== this.secuenciaRecuperacion) return;
        this.procesandoRecuperacion.set(false);
        this.errorRecuperacion.set(error.status === 429
          ? 'Espera antes de volver a solicitar un código.'
          : 'No fue posible procesar la solicitud. Inténtalo nuevamente más tarde.');
      }
    });
  }

  confirmarRecuperacion(): void {
    if (this.procesandoRecuperacion()) return;
    if (!/^[0-9]{6}$/.test(this.codigoRecuperacion)) {
      this.errorRecuperacion.set('Ingresa el código de 6 dígitos.');
      return;
    }
    if (this.nuevaContrasena.length < 8 || this.nuevaContrasena.length > 255 || !/\p{L}/u.test(this.nuevaContrasena) || !/\p{N}/u.test(this.nuevaContrasena) || !/[^\p{L}\p{N}\s]/u.test(this.nuevaContrasena)) {
      this.errorRecuperacion.set('La contraseña debe tener al menos 8 caracteres e incluir letras, números y un símbolo.');
      return;
    }
    if (this.nuevaContrasena !== this.confirmarContrasena) {
      this.errorRecuperacion.set('La nueva contraseña y su confirmación no coinciden.');
      return;
    }

    const secuencia = this.secuenciaRecuperacion;
    this.procesandoRecuperacion.set(true);
    this.errorRecuperacion.set('');
    this.autenticacionAdmin.confirmarRecuperacion(
      this.nombreUsuarioRecuperacion.trim(),
      this.codigoRecuperacion,
      this.nuevaContrasena,
      this.confirmarContrasena
    ).subscribe({
      next: (resultado) => {
        if (secuencia !== this.secuenciaRecuperacion) return;
        this.procesandoRecuperacion.set(false);
        this.usuario = this.nombreUsuarioRecuperacion.trim();
        this.contrasena = '';
        this.exito.set(resultado.mensaje);
        this.cerrarRecuperacion();
      },
      error: (error: HttpErrorResponse) => {
        if (secuencia !== this.secuenciaRecuperacion) return;
        this.procesandoRecuperacion.set(false);
        this.errorRecuperacion.set(typeof error.error?.mensaje === 'string'
          ? error.error.mensaje
          : 'No se pudo cambiar la contraseña. Si el código sigue vigente, puedes volver a intentarlo.');
      }
    });
  }

  cancelarRecuperacion(): void {
    if (this.procesandoRecuperacion()) return;
    this.secuenciaRecuperacion++;
    this.cerrarRecuperacion();
  }

  private cerrarRecuperacion(): void {
    this.mostrarSolicitudRecuperacion.set(false);
    this.mostrarConfirmacionRecuperacion.set(false);
    this.procesandoRecuperacion.set(false);
    this.errorRecuperacion.set('');
    this.mensajeRecuperacion.set('');
    this.codigoRecuperacion = '';
    this.nuevaContrasena = '';
    this.confirmarContrasena = '';
  }
  
}
