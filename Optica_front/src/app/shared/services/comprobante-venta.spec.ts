import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { crearComprobanteVenta } from './comprobante-venta-pdf';
import { ComprobanteVentaService } from './comprobante-venta.service';
import type { Venta } from '../../Componentes/registro-venta/registro-venta.service';

const venta: Venta = { idVenta: 42, fecha: '2026-11-04T15:30:00', total: 25980,
  nombreCliente: 'Ana Pérez', rutCliente: '12.345.678-5', productos: [
    { productoId: 1, nombre: 'Armazón óptico Café', cantidad: 2, precioUnitario: 12990, subtotal: 25980 }
  ] };

afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals(); });

describe('Comprobante de venta PDF', () => {
  it('genera un PDF con los importes confirmados y el aviso legal', async () => {
    const pdf = await crearComprobanteVenta(venta);
    const contenido = pdf.output();
    expect(contenido.startsWith('%PDF-')).toBe(true);
    expect(contenido).toContain('25.980');
    expect(contenido).toContain('12.990');
    expect(contenido).toContain('04/11/2026 15:30');
    expect(contenido).toContain('No es un documento tributario legal.');
    expect(contenido).toContain('12.345.678-5');
    expect(pdf.getNumberOfPages()).toBe(1);
  });

  it('pagina ventas extensas y conserva el aviso en todas las páginas', async () => {
    const pdf = await crearComprobanteVenta({ ...venta, rutCliente: null,
      productos: Array.from({ length: 80 }, (_, i) => ({ ...venta.productos[0],
        nombre: `Producto ${i + 1} con descripción extensa de armazón y accesorios`.repeat(3) })) });
    expect(pdf.getNumberOfPages()).toBeGreaterThan(3);
    expect(pdf.output()).toContain('sin cliente vinculado');
    expect(pdf.output().match(/No es un documento tributario legal\./g)?.length)
      .toBe(pdf.getNumberOfPages() * 2);
  });

  it('descarga un archivo PDF con el número de venta y retira el enlace temporal', async () => {
    const crearUrl = vi.fn((_blob: Blob) => 'blob:comprobante');
    vi.stubGlobal('URL', class extends URL { static override createObjectURL = crearUrl; static override revokeObjectURL = vi.fn(); });
    let nombre = '';
    const clic = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function(this: HTMLAnchorElement) {
      nombre = this.download;
      expect(this.href).toBe('blob:comprobante');
      expect(this.isConnected).toBe(true);
    });
    const servicio = TestBed.inject(ComprobanteVentaService);
    await servicio.descargar(venta);
    expect(clic).toHaveBeenCalledOnce();
    expect(nombre).toBe('comprobante-venta-42.pdf');
    expect(crearUrl.mock.calls[0][0]).toBeInstanceOf(Blob);
    expect((crearUrl.mock.calls[0][0] as Blob).type).toBe('application/pdf');
    expect(document.querySelector('a[download="comprobante-venta-42.pdf"]')).toBeNull();
    expect(servicio.descargando()).toBeNull();
    expect(servicio.error()).toBe('');
  });

  it('abre la vista previa de una venta en una pestaña y permite descargar por separado', async () => {
    vi.stubGlobal('URL', class extends URL { static override createObjectURL = vi.fn(() => 'blob:vista'); static override revokeObjectURL = vi.fn(); });
    const ventana = { opener: window, location: { href: '' }, close: vi.fn() };
    vi.spyOn(window, 'open').mockReturnValue(ventana as unknown as Window);
    const servicio = TestBed.inject(ComprobanteVentaService);
    await servicio.mostrar(venta);
    expect(ventana.location.href).toBe('blob:vista');
    expect(ventana.opener).toBeNull();
    expect(ventana.close).not.toHaveBeenCalled();
    expect(servicio.error()).toBe('');
  });

  it('informa un fallo de descarga y permite reintentar sin registrar otra venta', async () => {
    vi.stubGlobal('URL', class extends URL { static override createObjectURL = vi.fn(() => { throw new Error('fallo'); }); });
    const servicio = TestBed.inject(ComprobanteVentaService);
    await servicio.descargar(venta);
    expect(servicio.error()).toContain('Vuelve a intentar');
    expect(servicio.descargando()).toBeNull();
  });
});
