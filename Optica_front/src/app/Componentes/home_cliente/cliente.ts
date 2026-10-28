import { isPlatformBrowser } from '@angular/common';
import { Component, DestroyRef, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AutenticacionClienteService } from '../login-cliente/autenticacion-cliente.service';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-cliente',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './cliente.html',
  styleUrl: './cliente.css'
})
export class ClienteLayoutComponent implements OnInit {
  readonly autenticacion = inject(AutenticacionClienteService);
  readonly cerrandoSesion = signal(false);
  readonly errorSesion = signal('');
  private readonly router = inject(Router);
  private readonly plataforma = inject(PLATFORM_ID);
  private readonly destroyRef = inject(DestroyRef);

  cerrarSesion(): void {
    if (this.cerrandoSesion()) return;
    this.cerrandoSesion.set(true);
    this.errorSesion.set('');
    this.autenticacion.cerrarSesion().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.cerrandoSesion.set(false);
        void this.router.navigateByUrl('/inicio');
      },
      error: () => {
        this.cerrandoSesion.set(false);
        this.errorSesion.set('No se pudo cerrar sesión. Inténtalo nuevamente.');
      }
    });
  }

  ngOnInit(): void {
    if (!isPlatformBrowser(this.plataforma) || this.autenticacion.cliente()) return;
    this.autenticacion.verificarSesion().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      // Sin sesión, se conserva la navegación pública.
      error: () => {}
    });
  }
}
