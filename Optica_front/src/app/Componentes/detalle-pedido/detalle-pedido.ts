import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs';
import { PedidoService, PedidoResponse } from '../pedido/pedido.service';

@Component({
  selector: 'app-detalle-pedido',
  imports: [RouterLink, DatePipe],
  templateUrl: './detalle-pedido.html',
  styleUrl: './detalle-pedido.css'
})
export class DetallePedidoComponent implements OnInit, OnDestroy {
  protected readonly pedido = signal<PedidoResponse | null>(null);
  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly enProceso = signal(false);
  protected readonly pedidoPendienteConfirmacion = signal(false);
  protected readonly mensajeTemporal = signal<{ tipo: 'success' | 'warning' | 'error'; texto: string } | null>(null);
  private temporizadorMensaje: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly pedidoService: PedidoService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (isNaN(id)) {
      this.error.set('ID de pedido inválido.');
      this.cargando.set(false);
      return;
    }

    this.pedidoService.obtenerPedidoPorId(id).subscribe({
      next: (pedido) => {
        this.pedido.set(pedido);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el pedido.');
        this.cargando.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    if (this.temporizadorMensaje) clearTimeout(this.temporizadorMensaje);
  }

  protected cambiarEstado(pedido: PedidoResponse, event: Event): void {
    const select = event.target as HTMLSelectElement;
    const nuevoEstado = select.value;

    if (this.enProceso()) {
      select.value = pedido.estado;
      return;
    }

    if (nuevoEstado === 'Listo') {
      select.value = pedido.estado;
      this.pedidoPendienteConfirmacion.set(true);
      return;
    }

    this.actualizarEstado(pedido, nuevoEstado, select);
  }

  protected cancelarConfirmacionListo(): void {
    this.pedidoPendienteConfirmacion.set(false);
  }

  protected confirmarPedidoListo(): void {
    const pedido = this.pedido();
    this.pedidoPendienteConfirmacion.set(false);
    if (!pedido || this.enProceso()) return;
    this.actualizarEstado(pedido, 'Listo');
  }

  private actualizarEstado(pedido: PedidoResponse, nuevoEstado: string, select?: HTMLSelectElement): void {
    this.enProceso.set(true);

    this.pedidoService.actualizarEstado(pedido.idPedido, nuevoEstado).pipe(finalize(() => {
      this.enProceso.set(false);
    })).subscribe({
      next: (res) => {
        this.pedido.set({ ...pedido, estado: res.estado });
        if (nuevoEstado === 'Listo') {
          if (res.estadoNotificacion === 'Enviada') {
            this.mostrarMensaje('success', 'El pedido se actualizó y se envió el correo al cliente exitosamente.');
          } else {
            this.mostrarMensaje('warning', 'El pedido se actualizó correctamente, pero no se pudo enviar el correo al cliente.');
          }
        }
      },
      error: (error: unknown) => {
        if (select) select.value = pedido.estado;
        this.mostrarMensaje('error', this.obtenerMensajeError(error, 'No se pudo actualizar el estado del pedido'));
      }
    });
  }

  protected cerrarMensaje(): void {
    this.mensajeTemporal.set(null);
    if (this.temporizadorMensaje) clearTimeout(this.temporizadorMensaje);
    this.temporizadorMensaje = null;
  }

  private mostrarMensaje(tipo: 'success' | 'warning' | 'error', texto: string): void {
    if (this.temporizadorMensaje) clearTimeout(this.temporizadorMensaje);
    this.mensajeTemporal.set({ tipo, texto });
    this.temporizadorMensaje = setTimeout(() => this.mensajeTemporal.set(null), 5000);
  }

  private obtenerMensajeError(error: unknown, mensajePredeterminado: string): string {
    if (typeof error !== 'object' || error === null || !('error' in error)) return mensajePredeterminado;
    const cuerpo = error.error;
    if (typeof cuerpo !== 'object' || cuerpo === null || !('mensaje' in cuerpo)) return mensajePredeterminado;
    return typeof cuerpo.mensaje === 'string' ? cuerpo.mensaje : mensajePredeterminado;
  }
}
