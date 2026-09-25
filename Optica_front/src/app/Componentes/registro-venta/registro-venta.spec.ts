import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';
import { RegistroVentaComponent, inicioPeriodo, productosMasVendidos, PeriodoVentas } from './registro-venta';
import { RegistroVentaService, ProductoCaja, Venta } from './registro-venta.service';

describe('Registro de ventas', () => {
  const producto = { idProducto: 1, codigoProducto: 'A', nombre: 'Lentes', precio: 100, cantidad: 1 };
  const servicio = { listar: vi.fn(), buscar: vi.fn(), crear: vi.fn() };

  beforeEach(async () => {
    vi.resetAllMocks();
    servicio.listar.mockReturnValue(of([]));
    servicio.buscar.mockReturnValue(of(producto));
    servicio.crear.mockReturnValue(of({ idVenta: 1, fecha: '', total: 200, productos: [] }));
    await TestBed.configureTestingModule({
      imports: [RegistroVentaComponent],
      providers: [{ provide: RegistroVentaService, useValue: servicio }]
    }).compileComponents();
  });

  function preparar() {
    const fixture = TestBed.createComponent(RegistroVentaComponent);
    fixture.detectChanges();
    const dom = fixture.nativeElement as HTMLElement;
    const agregar = (codigo: string) => {
      const input = dom.querySelector<HTMLInputElement>('#codigo')!;
      input.value = codigo;
      input.dispatchEvent(new Event('input'));
      dom.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
      fixture.detectChanges();
    };
    const registrar = () => Array.from(dom.querySelectorAll('button')).find(b => b.textContent?.includes('Registrar compra'))!;
    return { fixture, dom, agregar, registrar };
  }

  it('acumula códigos repetidos y registra todos los productos juntos', () => {
    const { dom, agregar, registrar } = preparar();
    agregar('A'); agregar('A');
    expect(servicio.crear).not.toHaveBeenCalled();
    expect(dom.querySelectorAll('.linea').length).toBe(1);
    expect(dom.querySelector('.total')?.textContent).toContain('200');
    registrar().click();
    expect(servicio.crear).toHaveBeenCalledWith([{ codigoProducto: 'A', cantidad: 2 }]);
  });

  it('impide confirmar mientras quedan códigos pendientes', () => {
    const respuesta = new Subject<ProductoCaja>();
    servicio.buscar.mockReturnValue(respuesta);
    const { fixture, agregar, registrar } = preparar();
    agregar('A');
    expect(registrar().disabled).toBe(true);
    respuesta.next(producto); respuesta.complete(); fixture.detectChanges();
    expect(registrar().disabled).toBe(false);
  });

  it('rechaza códigos inexistentes sin añadir productos', () => {
    servicio.buscar.mockReturnValue(throwError(() => ({ error: { mensaje: 'No existe' } })));
    const { dom, agregar, registrar } = preparar();
    agregar('X');
    expect(dom.querySelectorAll('.linea').length).toBe(0);
    expect(dom.textContent).toContain('No existe');
    expect(registrar().disabled).toBe(true);
  });
  it('permite cambiar el top y el período desde la pantalla de ventas', () => {
    const ahora = new Date();
    const anterior = new Date(ahora);
    anterior.setDate(anterior.getDate() - 15);
    servicio.listar.mockReturnValue(of([
      { idVenta: 1, fecha: ahora.toISOString(), total: 12, productos:
        Array.from({ length: 12 }, (_, i) => ({ productoId: i + 1, nombre: `Producto ${i + 1}`, cantidad: 1, precioUnitario: 1, subtotal: 1 })) },
      { idVenta: 2, fecha: anterior.toISOString(), total: 100, productos:
        [{ productoId: 99, nombre: 'Producto anterior', cantidad: 100, precioUnitario: 1, subtotal: 100 }] }
    ]));
    const { fixture, dom } = preparar();
    expect(dom.querySelectorAll('.tabla-rank tbody tr')).toHaveLength(5);
    const limite = dom.querySelector<HTMLSelectElement>('#limite-rank')!;
    limite.value = '10'; limite.dispatchEvent(new Event('change')); fixture.detectChanges();
    expect(dom.querySelectorAll('.tabla-rank tbody tr')).toHaveLength(10);
    expect(dom.querySelector('.tabla-rank')?.textContent).not.toContain('Producto anterior');
    const periodo = dom.querySelector<HTMLSelectElement>('#periodo-rank')!;
    periodo.value = 'mes'; periodo.dispatchEvent(new Event('change')); fixture.detectChanges();
    expect(dom.querySelector('.tabla-rank tbody tr')?.textContent).toContain('Producto anterior');
  });

  it('distingue una carga fallida de un período sin ventas y permite reintentar', () => {
    servicio.listar.mockReturnValue(throwError(() => new Error('Sin conexión')));
    const { fixture, dom } = preparar();
    expect(dom.querySelector('.rank-productos')?.textContent).toContain('No se pudo cargar el rank');
    servicio.listar.mockReturnValue(of([]));
    dom.querySelector<HTMLButtonElement>('.rank-productos button')!.click();
    fixture.detectChanges();
    expect(dom.querySelector('.rank-productos')?.textContent).toContain('No hay ventas en el período seleccionado');
  });

});

const hasta = new Date(2026, 8, 24, 12);
function venta(fecha: Date, id: number, cantidad: number, precio = 100): Venta {
  return { idVenta: 1, fecha: fecha.toISOString(), total: cantidad * precio,
    productos: [{ productoId: id, nombre: `Producto ${id}`, cantidad,
      precioUnitario: precio, subtotal: cantidad * precio }] };
}

describe('Productos más vendidos', () => {
  it('suma unidades entre ventas y ordena por volumen, independientemente del ingreso', () => {
    const resultado = productosMasVendidos([
      venta(hasta, 1, 2, 10000), venta(hasta, 2, 3), venta(hasta, 2, 4)
    ], 'semana', 5, hasta);
    expect(resultado.map(p => [p.productoId, p.unidades])).toEqual([[2, 7], [1, 2]]);
  });

  for (const periodo of ['semana', 'mes', 'semestre', 'anio'] as PeriodoVentas[]) {
    it(`respeta ambos límites de ${periodo} y excluye ventas futuras`, () => {
      const inicio = inicioPeriodo(periodo, hasta);
      const resultado = productosMasVendidos([
        venta(new Date(inicio.getTime() - 1), 1, 50), venta(inicio, 2, 2),
        venta(hasta, 2, 3), venta(new Date(hasta.getTime() + 1), 3, 50)
      ], periodo, 5, hasta);
      expect(resultado.map(p => [p.productoId, p.unidades])).toEqual([[2, 5]]);
    });
  }

  it('usa 7 días, 1 mes, 6 meses y 1 año', () => {
    expect(inicioPeriodo('semana', hasta)).toEqual(new Date(2026, 8, 17, 12));
    expect(inicioPeriodo('mes', hasta)).toEqual(new Date(2026, 7, 24, 12));
    expect(inicioPeriodo('semestre', hasta)).toEqual(new Date(2026, 2, 24, 12));
    expect(inicioPeriodo('anio', hasta)).toEqual(new Date(2025, 8, 24, 12));
  });

  it('ajusta fines de mes y años bisiestos sin saltarse febrero', () => {
    expect(inicioPeriodo('mes', new Date(2026, 2, 31, 12))).toEqual(new Date(2026, 1, 28, 12));
    expect(inicioPeriodo('anio', new Date(2024, 1, 29, 12))).toEqual(new Date(2023, 1, 28, 12));
  });

  it('limita el ranking a 5 o 10 y desempata por identificador', () => {
    const ventas = Array.from({ length: 12 }, (_, i) => venta(hasta, 12 - i, 1));
    for (const limite of [5, 10] as const) {
      expect(productosMasVendidos(ventas, 'anio', limite, hasta).map(p => p.productoId))
        .toEqual(Array.from({ length: limite }, (_, i) => i + 1));
    }
  });

  it('devuelve solo los productos disponibles y admite períodos sin ventas', () => {
    expect(productosMasVendidos([], 'mes', 10, hasta)).toEqual([]);
    expect(productosMasVendidos([venta(hasta, 1, 1)], 'mes', 10, hasta)).toHaveLength(1);
  });
});
