import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';
import { RegistroVentaComponent } from './registro-venta';
import { RegistroVentaService, ProductoCaja } from './registro-venta.service';

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
});
