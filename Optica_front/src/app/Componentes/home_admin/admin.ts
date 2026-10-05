import { AutenticacionAdminService } from '../login-admin/autenticacion-admin.service';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { catchError, EMPTY, exhaustMap, interval, startWith } from 'rxjs';
import { BuscarProductoService, Producto } from '../buscar-producto/buscar-producto.service';
import { NotificacionesAdminService } from '../../shared/services/notificaciones-admin.service';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin.html',
  styleUrl: './admin.css'
})
export class AdminLayoutComponent {
  protected readonly sesion = inject(AutenticacionAdminService);
  private readonly router = inject(Router);
  private readonly productosService = inject(BuscarProductoService);
  protected readonly servicioNotificaciones = inject(NotificacionesAdminService);
  protected readonly errorSesion = signal('');
  protected readonly notificacionesAbiertas = signal(false);
  protected readonly errorInventario = signal(false);
  protected readonly notificaciones = this.servicioNotificaciones.notificaciones;
  protected readonly cantidadNotificaciones = this.servicioNotificaciones.cantidad;
  private stocksAnteriores = new Map<number, number>();
  private inventarioInicializado = false;

  constructor() {
    interval(15_000).pipe(
      startWith(0),
      exhaustMap(() => this.productosService.buscar('').pipe(
        catchError(() => {
          this.errorInventario.set(true);
          return EMPTY;
        })
      )),
      takeUntilDestroyed()
    ).subscribe(productos => this.procesarInventario(productos));
  }

  protected alternarNotificaciones(): void {
    this.notificacionesAbiertas.update(abiertas => !abiertas);
  }

  protected eliminarNotificacion(id: number): void {
    this.servicioNotificaciones.eliminar(id);
  }

  private procesarInventario(productos: Producto[]): void {
    this.errorInventario.set(false);
    if (!this.inventarioInicializado) {
      this.stocksAnteriores = new Map(productos.map(producto => [producto.id, producto.stock]));
      this.inventarioInicializado = true;
      return;
    }

    for (const producto of productos) {
      const stockAnterior = this.stocksAnteriores.get(producto.id);
      if (stockAnterior !== undefined && stockAnterior > producto.stockMinimo && producto.stock <= producto.stockMinimo) {
        this.servicioNotificaciones.agregar({
          titulo: `Bajo stock: ${producto.nombre}`,
          mensaje: `Quedan ${producto.stock}; mínimo ${producto.stockMinimo}.`,
          tipo: 'advertencia'
        });
      }
    }

    this.stocksAnteriores = new Map(productos.map(producto => [producto.id, producto.stock]));
  }

  protected salir(): void {
    this.sesion.cerrarSesion().subscribe({
      next: () => { void this.router.navigate(['/admin']); },
      error: () => this.errorSesion.set('No se pudo cerrar la sesión. Inténtalo nuevamente.')
    });
  }
}