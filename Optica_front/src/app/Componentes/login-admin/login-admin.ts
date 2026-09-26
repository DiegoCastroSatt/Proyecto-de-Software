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
  
  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private autenticacionAdmin: AutenticacionAdminService
  ) {}

  onLogin(): void{
    this.error.set('');

    this.autenticacionAdmin.iniciarSesion(this.usuario, this.contrasena).subscribe({
      next: () => {
        sessionStorage.setItem('isAdmin', 'true');

        const destino = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/admin/panel';
        void this.router.navigateByUrl(destino);
      },
      error: (error: HttpErrorResponse) => {
        this.error.set(error.status === 401
          ? 'Nombre de usuario o contraseña incorrectos.'
          : 'No se pudo conectar con el servidor. Intenta nuevamente.');
      }
    });

  }
  
}
