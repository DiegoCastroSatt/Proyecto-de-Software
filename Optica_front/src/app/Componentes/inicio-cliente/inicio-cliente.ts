import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, afterNextRender, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { BuscarProductoService, Producto } from '../buscar-producto/buscar-producto.service';

@Component({
  selector: 'app-inicio-cliente',
  imports: [RouterLink, CurrencyPipe],
  templateUrl: './inicio-cliente.html',
  styleUrl: './inicio-cliente.css'
})
export class InicioClienteComponent implements OnInit {
  private readonly servicio = inject(BuscarProductoService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly productos = signal<Producto[]>([]);
  protected readonly indice = signal(0);
  protected readonly cargando = signal(true);
  protected readonly error = signal(false);
  protected readonly imagenFallida = signal(false);
  protected readonly actual = computed(() => this.productos()[this.indice()]);

  constructor() {
    // Se inicia solo en el navegador, después del renderizado inicial.
    afterNextRender(() => {
      const temporizador = window.setInterval(() => {
        if (!document.hidden && !this.cargando() && !this.error() && this.productos().length > 1) {
          this.seleccionar(this.indice() + 1);
        }
      }, 5000);
      this.destroyRef.onDestroy(() => window.clearInterval(temporizador));
    });
  }

  ngOnInit(): void { this.cargar(); }

  protected cargar(): void {
    this.cargando.set(true);
    this.error.set(false);
    this.servicio.buscar('').pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: productos => {
        this.productos.set(productos.filter(p => p.estado === 'Disponible' && p.rutaImagen?.trim()));
        this.seleccionar(0);
        this.cargando.set(false);
      },
      error: () => { this.error.set(true); this.cargando.set(false); }
    });
  }

  protected seleccionar(indice: number): void {
    const total = this.productos().length;
    this.indice.set(total ? (indice + total) % total : 0);
    this.imagenFallida.set(false);
  }

  protected imagenUrl(ruta: string): string {
    return /^https?:\/\//i.test(ruta) ? ruta : `http://localhost:8080${ruta.startsWith('/') ? '' : '/'}${ruta}`;
  }
}
