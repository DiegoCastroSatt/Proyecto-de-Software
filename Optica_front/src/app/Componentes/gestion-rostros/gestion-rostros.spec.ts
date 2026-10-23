import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { GestionRostrosComponent } from './gestion-rostros';
import { TiposRostroService, TipoRostro } from '../../shared/services/tipos-rostro.service';
import { TiposMarcoService } from '../../shared/services/tipos-marco.service';

const rostro: TipoRostro = { id: 1, codigo: 'ovalado', nombre: 'Ovalado', rasgos: 'Rasgos', consejo: 'Consejo', activo: true, rutaImagen: '/assets/images/recomendador/rostro-ovalado.png', marcos: [1] };
describe('Gestión de rostros', () => {
  function crear() {
    const servicio = { listar: vi.fn(() => of([{ ...rostro }])), crear: vi.fn(() => of({ ...rostro, id: 2 })),
      cambiarEstado: vi.fn(() => of(undefined)), asociarMarcos: vi.fn(() => of(undefined)), borrar: vi.fn(() => of(undefined)) };
    TestBed.configureTestingModule({ imports: [GestionRostrosComponent], providers: [provideRouter([]), 
      { provide: TiposRostroService, useValue: servicio }, { provide: TiposMarcoService, useValue: { listar: () => of([{ id: 1, nombre: 'Redondo', activo: true }]) } },
    ] });
    const fixture = TestBed.createComponent(GestionRostrosComponent); fixture.detectChanges();
    return { fixture, componente: fixture.componentInstance, servicio };
  }

  it('lista imagen y permite desactivar conservando asociaciones', () => {
    const { fixture, componente, servicio } = crear();
    expect(fixture.nativeElement.querySelector('.retrato').src).toContain('/assets/images/recomendador/rostro-ovalado.png');
    componente.alternar(rostro);
    expect(servicio.cambiarEstado).toHaveBeenCalledWith(1, false);
    expect(componente.rostros()[0].activo).toBe(false); expect(componente.rostros()[0].marcos).toEqual([1]);
  });

  it('pide confirmación de borrado y retira el elemento tras guardar', () => {
    const { componente, servicio } = crear(); componente.borrar(rostro); expect(servicio.borrar).not.toHaveBeenCalled();
    componente.confirmarBorrado.set(1); componente.borrar(rostro); expect(componente.rostros()).toEqual([]);
  });

  it('permite cambiar los marcos recomendados', () => {
    const { componente, servicio } = crear(); componente.editarAsociaciones(rostro); componente.asociaciones.clear();
    componente.guardarAsociaciones(rostro);
    expect(servicio.asociarMarcos).toHaveBeenCalledWith(1, []); expect(componente.rostros()[0].marcos).toEqual([]);
  });

  it('conserva la fila y muestra el error si falla el borrado', () => {
    const { componente, servicio } = crear(); servicio.borrar.mockReturnValue(throwError(() => ({ error: { mensaje: 'Falló' } })));
    componente.confirmarBorrado.set(1); componente.borrar(rostro);
    expect(componente.rostros().length).toBe(1); expect(componente.error()).toBe('Falló');
  });

  it('envía imagen, descripción y asociaciones al crear', () => {
    const { componente, servicio } = crear(); const imagen = new File(['imagen'], 'rostro.png', { type: 'image/png' });
    componente.nombre = 'Triangular'; componente.rasgos = 'Rasgos'; componente.consejo = 'Consejo'; componente.imagen = imagen;
    componente.marcosSeleccionados.add(1); componente.crear(document.createElement('input'));
    expect(servicio.crear).toHaveBeenCalledWith({ nombre: 'Triangular', rasgos: 'Rasgos', consejo: 'Consejo', imagen, marcos: [1] });
    expect(componente.rostros().length).toBe(2);
  });
});
