import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, of, Subject, switchMap } from 'rxjs';
import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PedidoService, PedidoResponse } from './pedido.service';

@Component({
  selector: 'app-pedido',
  imports: [RouterLink, DatePipe],
  templateUrl: './pedido.html',
  styleUrl: './pedido.css'
})
export class PedidoComponent implements OnInit {
  protected readonly pedidos = signal<PedidoResponse[]>([]);
  protected readonly pagina = signal(1);
  protected readonly haySiguiente = signal(false);
  protected readonly cargando = signal(false);
  protected readonly error = signal(false);
  private termino = '';
  private readonly solicitudes = new Subject<{ pagina: number; termino: string }>();

  constructor(private readonly pedidoService: PedidoService) {
    this.solicitudes.pipe(
      switchMap(({ pagina, termino }) => this.pedidoService.obtenerPagina(pagina, termino).pipe(
        catchError(() => of(null))
      )),
      takeUntilDestroyed()
    ).subscribe(resultado => {
      this.cargando.set(false);
      this.error.set(resultado === null);
      this.pedidos.set(resultado?.pedidos ?? []);
      this.haySiguiente.set(resultado?.haySiguiente ?? false);
    });
  }

  ngOnInit(): void {
    this.cambiarPagina(1);
  }

  protected buscar(termino: string): void {
    this.termino = termino.trim();
    this.cambiarPagina(1);
  }

  protected cambiarPagina(pagina: number): void {
    this.pagina.set(pagina);
    this.cargando.set(true);
    this.error.set(false);
    this.solicitudes.next({ pagina, termino: this.termino });
  }
}
