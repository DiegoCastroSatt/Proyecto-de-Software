import { AutenticacionAdminService } from '../login-admin/autenticacion-admin.service';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin.html',
  styleUrl: './admin.css'
})
export class AdminLayoutComponent {
  protected readonly sesion = inject(AutenticacionAdminService);
  private readonly router = inject(Router);
  protected readonly errorSesion = signal('');
  protected salir(): void {
    this.sesion.cerrarSesion().subscribe({
      next: () => { void this.router.navigate(['/admin']); },
      error: () => this.errorSesion.set('No se pudo cerrar la sesión. Inténtalo nuevamente.')
    });
  }
}