import { provideRouter } from '@angular/router';
import { TiposRostroService } from '../../shared/services/tipos-rostro.service';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { GestionMarcosComponent } from './gestion-marcos';
import { TiposMarcoService, TipoMarco } from '../../shared/services/tipos-marco.service';

const marco: TipoMarco = { id: 1, codigo: 'redondo', nombre: 'Redondo', categoria: 'Redondo', activo: true, rostros: ['ovalado'], stock: 3 };

describe('Inventario de marcos', () => {
  function crear() {
    const servicio = { productosVinculables: vi.fn(() => of([{ id: 10, nombre: 'Armazón Ray-Ban', codigo: 'RB1', categoria: 'Armazones', stock: 3, estado: 'Disponible', idTipoMarco: 1 }, { id: 11, nombre: 'Armazón nuevo', codigo: 'RB2', categoria: 'Armazones', stock: 2, estado: 'Disponible', idTipoMarco: null }])), listar: vi.fn(() => of([{ ...marco }])), crear: vi.fn(() => of({ ...marco, id: 2 })),
      cambiarEstado: vi.fn(() => of(undefined)), borrar: vi.fn(() => of(undefined)), vincularProductos: vi.fn(() => of([{ ...marco }])) };
    TestBed.configureTestingModule({ imports: [GestionMarcosComponent], providers: [provideRouter([]), { provide: TiposRostroService, useValue: { activos: () => of([{ id: 1, codigo: 'ovalado', nombre: 'Ovalado' }]) } }, { provide: TiposMarcoService, useValue: servicio }] });
    const fixture = TestBed.createComponent(GestionMarcosComponent); fixture.detectChanges();
    return { fixture, componente: fixture.componentInstance, servicio };
  }

  it('lista el SVG y permite desactivar sin borrar el tipo', () => {
    const { fixture, componente, servicio } = crear();
    expect(fixture.nativeElement.querySelector('.lista img').src).toContain('/api/Marcos/1/imagen');
    componente.alternar(componente.marcos()[0]); fixture.detectChanges();
    expect(servicio.cambiarEstado).toHaveBeenCalledWith(1, false);
    expect(componente.marcos()[0].activo).toBe(false);
    expect(servicio.borrar).not.toHaveBeenCalled();
  });

  it('requiere confirmar el borrado y retira el elemento tras guardarlo', () => {
    const { componente, servicio } = crear();
    componente.borrar(marco); expect(servicio.borrar).not.toHaveBeenCalled();
    componente.confirmarBorrado.set(1); componente.borrar(marco);
    expect(servicio.borrar).toHaveBeenCalledWith(1); expect(componente.marcos()).toEqual([]);
  });

  it('envía nombre, categoría, SVG y rostros al crear', () => {
    const { componente, servicio } = crear();
    componente.nombre = 'Nuevo'; componente.categoria = 'Nueva'; componente.svg = '<svg/>';
    componente.rostrosSeleccionados = new Set(['ovalado']);
    componente.crear(document.createElement('input'));
    expect(servicio.crear).toHaveBeenCalledWith({ nombre: 'Nuevo', categoria: 'Nueva', svg: '<svg/>', rostros: ['ovalado'] });
    expect(componente.marcos().length).toBe(2); expect(componente.svg).toContain('<svg');
  });

  it('conserva el estado visible si falla la desactivación', () => {
    const { componente, servicio } = crear();
    servicio.cambiarEstado.mockReturnValue(throwError(() => ({ error: { mensaje: 'Falló' } })));
    componente.alternar(marco); expect(componente.marcos()[0].activo).toBe(true);
    expect(componente.error()).toBe('Falló'); expect(componente.ocupado()).toBeNull();
  });
  it('permite crear un marco sin cargar archivos ni conocer SVG', () => {
    const { componente, servicio, fixture } = crear();
    componente.nombre = 'Nuevo'; componente.categoria = 'Nueva';
    componente.crear(document.createElement('input'));
    expect(servicio.crear).toHaveBeenCalledWith(expect.objectContaining({ nombre: 'Nuevo', svg: expect.stringContaining('<svg') }));
    expect(fixture.nativeElement.querySelector('app-editor-marco')).toBeTruthy();
  });

  it('permite vincular armazones sin exigir que su categoría sea igual al diseño', () => {
    const { componente, servicio } = crear(); componente.abrirVinculos(marco);
    expect(componente.productosSeleccionados).toEqual(new Set([10]));
    componente.marcarProducto(11, true); componente.guardarVinculos(marco);
    expect(servicio.vincularProductos).toHaveBeenCalledWith(1, [10, 11]);
    expect(componente.vinculando()).toBeNull(); expect(componente.aviso()).toContain('no se duplicaron');
  });

  it('conserva la selección aunque se filtre la lista y permite desvincular', () => {
    const { componente, servicio } = crear(); componente.abrirVinculos(marco); componente.filtroProductos = 'nuevo';
    expect(componente.productosFiltrados.map(p => p.id)).toEqual([11]);
    componente.marcarProducto(10, false); componente.guardarVinculos(marco);
    expect(servicio.vincularProductos).toHaveBeenCalledWith(1, []);
  });

});
