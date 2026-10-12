import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PedidoService, PedidoResponse } from './pedido.service';

@Component({
  selector: 'app-pedido',
  imports: [RouterLink, DatePipe],
  templateUrl: './pedido.html',
  styleUrl: './pedido.css'
})
export class PedidoComponent implements OnInit, OnDestroy {
  protected readonly pedidos = signal<PedidoResponse[]>([]);
  protected readonly enProceso = signal<Set<number>>(new Set());
  protected readonly pedidoPendienteConfirmacion = signal<number | null>(null);
  protected readonly mensajeTemporal = signal<{ tipo: 'success' | 'warning' | 'error'; texto: string } | null>(null);
  private temporizadorMensaje: ReturnType<typeof setTimeout> | null = null;

  constructor(private readonly pedidoService: PedidoService) {}

  ngOnInit(): void {
    this.pedidoService.obtenerPedidos().subscribe({
      next: (pedidos) => this.pedidos.set(pedidos)
    });
  }

  ngOnDestroy(): void {
    if (this.temporizadorMensaje) clearTimeout(this.temporizadorMensaje);
  }

  protected cambiarEstado(pedido: PedidoResponse, event: Event): void {
    const select = event.target as HTMLSelectElement;
    const nuevoEstado = select.value;

    if (this.enProceso().has(pedido.idPedido)) {
      select.value = pedido.estado;
      return;
    }
    if (nuevoEstado === 'Listo') {
      select.value = pedido.estado;
      this.pedidoPendienteConfirmacion.set(pedido.idPedido);
      return;
    }

    this.actualizarEstado(pedido, nuevoEstado, select);
  }

  protected cancelarConfirmacionListo(): void {
    this.pedidoPendienteConfirmacion.set(null);
  }

  protected confirmarPedidoListo(): void {
    const idPedido = this.pedidoPendienteConfirmacion();
    const pedido = this.pedidos().find(item => item.idPedido === idPedido);
    this.pedidoPendienteConfirmacion.set(null);
    if (!pedido || this.enProceso().has(pedido.idPedido)) return;
    this.actualizarEstado(pedido, 'Listo');
  }

  private actualizarEstado(pedido: PedidoResponse, nuevoEstado: string, select?: HTMLSelectElement): void {
    this.enProceso.update(ids => new Set(ids).add(pedido.idPedido));
    
    this.pedidoService.actualizarEstado(pedido.idPedido, nuevoEstado).pipe(finalize(() => {
      this.enProceso.update(ids => { const actualizados = new Set(ids); actualizados.delete(pedido.idPedido); return actualizados; });
    })).subscribe({
      next: (res) => {
        this.pedidos.update(pedidos => 
          pedidos.map(p => p.idPedido === pedido.idPedido ? { ...p, estado: res.estado } : p)
        );
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
