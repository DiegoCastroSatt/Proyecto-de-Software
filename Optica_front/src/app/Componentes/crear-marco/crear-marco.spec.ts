import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { CrearMarcoComponent } from './crear-marco';
import { TiposMarcoService } from '../../shared/services/tipos-marco.service';
import { TiposRostroService } from '../../shared/services/tipos-rostro.service';

describe('Diseño y creación de marcos', () => {
  function crear() {
    const servicio = { listar: vi.fn(() => of([])), listarFormas: vi.fn(() => of([])), crear: vi.fn(() => of({ id: 2 })) };
    TestBed.configureTestingModule({ imports: [CrearMarcoComponent], providers: [provideRouter([]),
      { provide: TiposMarcoService, useValue: servicio },
      { provide: TiposRostroService, useValue: { activos: () => of([{ id: 1, codigo: 'ovalado', nombre: 'Ovalado' }]) } },
    ] });
    const fixture = TestBed.createComponent(CrearMarcoComponent); fixture.detectChanges();
    return { fixture, componente: fixture.componentInstance, servicio };
  }
  it('envía nombre, categoría, SVG y rostros al crear', () => {
    const { componente, servicio } = crear();
    componente.nombre = 'Nuevo'; componente.categoria = 'Nueva'; componente.svg = '<svg/>';
    componente.rostrosSeleccionados = new Set(['ovalado']);
    componente.crear(document.createElement('input'));
    expect(servicio.crear).toHaveBeenCalledWith({ nombre: 'Nuevo', categoria: 'Nueva', svg: '<svg/>', rostros: ['ovalado'] });
    expect(componente.aviso()).toContain('Marco creado'); expect(componente.svg).toContain('<svg');
  });

  it('permite crear un marco sin cargar archivos ni conocer SVG', () => {
    const { componente, servicio, fixture } = crear();
    componente.nombre = 'Nuevo'; componente.categoria = 'Nueva';
    componente.crear(document.createElement('input'));
    expect(servicio.crear).toHaveBeenCalledWith(expect.objectContaining({ nombre: 'Nuevo', svg: expect.stringContaining('<svg') }));
    expect(fixture.nativeElement.querySelector('app-editor-marco')).toBeTruthy();
  });
  it('conserva el diseño y el nombre cuando falla el guardado', () => {
    const { componente, servicio } = crear(); componente.nombre = 'Nuevo';
    servicio.crear.mockReturnValue(throwError(() => ({ error: { mensaje: 'Falló' } })));
    const svg = componente.svg; componente.crear(document.createElement('input'));
    expect(componente.error()).toBe('Falló'); expect(componente.nombre).toBe('Nuevo');
    expect(componente.svg).toBe(svg); expect(componente.guardando()).toBe(false);
  });
});
