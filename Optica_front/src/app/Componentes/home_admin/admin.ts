import { AutenticacionAdminService } from '../login-admin/autenticacion-admin.service';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { catchError, EMPTY, exhaustMap, interval, startWith } from 'rxjs';
import { BuscarProductoService, Producto } from '../buscar-producto/buscar-producto.service';

interface NotificacionStock {
  id: number;
  productoId: number;
  nombre: string;
  stock: number;
  stockMinimo: number;
}

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
  protected readonly errorSesion = signal('');
  protected readonly notificacionesAbiertas = signal(false);
  protected readonly errorNotificaciones = signal(false);
  protected readonly notificaciones = signal<NotificacionStock[]>([]);
  protected readonly cantidadNotificaciones = computed(() => this.notificaciones().length);
  private stocksAnteriores = new Map<number, number>();
  private inventarioInicializado = false;
  private siguienteIdNotificacion = 1;

  constructor() {
    interval(15_000).pipe(
      startWith(0),
      exhaustMap(() => this.productosService.buscar('').pipe(
        catchError(() => {
          this.errorNotificaciones.set(true);
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
    this.notificaciones.update(items => items.filter(item => item.id !== id));
  }

  private procesarInventario(productos: Producto[]): void {
    this.errorNotificaciones.set(false);
    if (!this.inventarioInicializado) {
      this.stocksAnteriores = new Map(productos.map(producto => [producto.id, producto.stock]));
      this.inventarioInicializado = true;
      return;
    }

    const nuevasNotificaciones: NotificacionStock[] = [];
    for (const producto of productos) {
      const stockAnterior = this.stocksAnteriores.get(producto.id);
      if (stockAnterior !== undefined && stockAnterior > producto.stockMinimo && producto.stock <= producto.stockMinimo) {
        nuevasNotificaciones.push({
          id: this.siguienteIdNotificacion++,
          productoId: producto.id,
          nombre: producto.nombre,
          stock: producto.stock,
          stockMinimo: producto.stockMinimo
        });
      }
    }

    this.stocksAnteriores = new Map(productos.map(producto => [producto.id, producto.stock]));
    if (nuevasNotificaciones.length) {
      this.notificaciones.update(items => [...nuevasNotificaciones, ...items]);
    }
  }

  protected salir(): void {
    this.sesion.cerrarSesion().subscribe({
      next: () => { void this.router.navigate(['/admin']); },
      error: () => this.errorSesion.set('No se pudo cerrar la sesión. Inténtalo nuevamente.')
    });
  }
}