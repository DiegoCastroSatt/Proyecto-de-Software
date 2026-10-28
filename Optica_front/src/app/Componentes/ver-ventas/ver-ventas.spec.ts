import { describe, expect, it } from 'vitest';
import { inicioPeriodo, productosMasVendidos } from './ver-ventas';
import { Venta } from '../registro-venta/registro-venta.service';

describe('Resumen de ventas', () => {
  it('calcula el inicio semanal restando siete días', () => {
    const hasta = new Date(2026, 9, 26, 12);

    expect(inicioPeriodo('semana', hasta)).toEqual(new Date(2026, 9, 19, 12));
  });

  it('calcula correctamente meses cuyo día no existe en el mes anterior', () => {
    const hasta = new Date(2026, 7, 31, 12);

    expect(inicioPeriodo('mes', hasta)).toEqual(new Date(2026, 6, 31, 12));
  });

  it('suma unidades por producto, agrupa productos eliminados y ordena con desempate estable', () => {
    const ventas: Venta[] = [
      { idVenta: 1, fecha: '2026-10-20T12:00:00', total: 100, productos: [
        { productoId: 2, nombre: 'Armazón B', cantidad: 2, precioUnitario: 50, subtotal: 100 },
        { productoId: null, nombre: 'Producto eliminado', cantidad: 1, precioUnitario: 10, subtotal: 10 }
      ] },
      { idVenta: 2, fecha: '2026-10-21T12:00:00', total: 100, productos: [
        { productoId: 1, nombre: 'Armazón A', cantidad: 2, precioUnitario: 50, subtotal: 100 },
        { productoId: null, nombre: 'Producto eliminado', cantidad: 3, precioUnitario: 10, subtotal: 30 }
      ] },
      { idVenta: 3, fecha: '2026-10-01T00:00:00', total: 500, productos: [
        { productoId: 9, nombre: 'Fuera de período', cantidad: 99, precioUnitario: 5, subtotal: 495 }
      ] }
    ];

    const top = productosMasVendidos(ventas, 'semana', 5, new Date('2026-10-26T12:00:00'));

    expect(top).toEqual([
      { productoId: 0, nombre: 'Producto eliminado', unidades: 4 },
      { productoId: 1, nombre: 'Armazón A', unidades: 2 },
      { productoId: 2, nombre: 'Armazón B', unidades: 2 }
    ]);
  });

  it('respeta el límite seleccionado del ranking', () => {
    const venta: Venta = {
      idVenta: 1,
      fecha: '2026-10-24T12:00:00',
      total: 90,
      productos: [1, 2, 3].map(productoId => ({
        productoId, nombre: `Producto ${productoId}`, cantidad: productoId, precioUnitario: 10, subtotal: productoId * 10
      }))
    };

    expect(productosMasVendidos([venta], 'semana', 10, new Date('2026-10-26T12:00:00'))).toHaveLength(3);
    expect(productosMasVendidos([venta], 'semana', 5, new Date('2026-10-26T12:00:00'))).toHaveLength(3);
  });
});
