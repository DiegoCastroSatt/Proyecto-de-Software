import { Injectable, signal } from '@angular/core';
import type { Venta } from '../../Componentes/registro-venta/registro-venta.service';

@Injectable({ providedIn: 'root' })
export class ComprobanteVentaService {
  readonly descargando = signal<number | null>(null);
  readonly error = signal('');

  descargar(venta: Venta): Promise<void> { return this.generar(venta, false); }
  mostrar(venta: Venta): Promise<void> { return this.generar(venta, true); }

  private async generar(venta: Venta, mostrar: boolean): Promise<void> {
    if (this.descargando() !== null) return;
    this.descargando.set(venta.idVenta);
    this.error.set('');
    let url: string | undefined;
    let enlace: HTMLAnchorElement | undefined;
    let ventana: Window | null = null;
    try {
      if (mostrar) {
        // Abre la pestaña durante el clic para evitar el bloqueo de ventanas emergentes.
        ventana = window.open('', '_blank');
        if (!ventana) throw new Error('No se pudo abrir la vista previa.');
        ventana.opener = null;
      }
      const { crearComprobanteVenta } = await import('./comprobante-venta-pdf');
      const pdf = await crearComprobanteVenta(venta);
      url = URL.createObjectURL(pdf.output('blob'));
      if (ventana) {
        ventana.location.href = url;
      } else {
        enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = `comprobante-venta-${venta.idVenta}.pdf`;
        document.body.appendChild(enlace);
        enlace.click();
      }
    } catch {
      ventana?.close();
      this.error.set('No se pudo abrir o descargar el comprobante PDF. Vuelve a intentar o usa la descarga directa.');
    } finally {
      enlace?.remove();
      if (url) setTimeout(() => URL.revokeObjectURL(url!), 60_000);
      this.descargando.set(null);
    }
  }
}
