import { formasLentes, configuracionDesdeCero, generarMarcoDesdeCero } from './diseno-desde-cero';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { EditorMarcoComponent } from './editor-marco';
import { ajustarDisenoMarco, ajustesDisenoIniciales, disenoMarcoInicial } from './diseno-marco';
import { TiposMarcoService } from '../../shared/services/tipos-marco.service';

const diseno = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g stroke="#222222" stroke-width="4"><polygon points="10,20 80,20 90,70"/><ellipse cx="170" cy="50" rx="30" ry="20"/></g></svg>';

describe('Editor visual de marcos', () => {
  function crear() {
    const servicio = { obtenerDiseno: vi.fn(() => of(diseno)) };
    TestBed.configureTestingModule({ imports: [EditorMarcoComponent], providers: [{ provide: TiposMarcoService, useValue: servicio }] });
    const fixture = TestBed.createComponent(EditorMarcoComponent);
    fixture.componentRef.setInput('disenos', [{ id: 51, nombre: 'Diseño nuevo' }]); fixture.detectChanges();
    return { fixture, componente: fixture.componentInstance, servicio };
  }

  it('obtiene las opciones de la base y conserva los elementos de un diseño nuevo', () => {
    const { fixture, componente, servicio } = crear();
    const emitir = vi.spyOn(componente.disenoChange, 'emit');
    expect(fixture.nativeElement.querySelector('select').textContent).toContain('Diseño nuevo');
    componente.elegir('51');
    expect(servicio.obtenerDiseno).toHaveBeenCalledWith(51);
    const generado = emitir.mock.calls.at(-1)![0];
    expect(generado).toContain('<polygon'); expect(generado).toContain('<ellipse');
    expect(componente.vistaPrevia()).toContain('data:image/svg+xml');
  });

  it('ajusta color, proporciones y grosor sin acumular cambios', () => {
    const ajustes = { ancho: 110, alto: 80, grosor: 6, color: '#ff0000' };
    const generado = ajustarDisenoMarco(diseno, ajustes);
    const raiz = new DOMParser().parseFromString(generado, 'image/svg+xml');
    expect(raiz.querySelector('[stroke]')?.getAttribute('stroke')).toBe('#ff0000');
    expect(raiz.querySelector('[stroke-width]')?.getAttribute('stroke-width')).toBe('6');
    expect(raiz.querySelector('g')?.getAttribute('transform')).toContain('scale(1.1 0.8)');
    expect(ajustarDisenoMarco(diseno, ajustes)).toBe(generado);
    expect(ajustarDisenoMarco(diseno, ajustesDisenoIniciales)).not.toBe(generado);
  });

  it('descarta una plantilla pendiente al volver al diseño simple', () => {
    const { componente, servicio } = crear(); const pendiente = new Subject<string>();
    servicio.obtenerDiseno.mockReturnValue(pendiente);
    componente.elegir('51'); componente.restablecer(); const vista = componente.vistaPrevia(); pendiente.next(diseno);
    expect(componente.vistaPrevia()).toBe(vista); expect(componente.elegido).toBe(''); expect(componente.cargando()).toBe(false);
  });

  it('mantiene un diseño válido cuando no se puede cargar una plantilla', () => {
    const { componente } = crear(); const vista = componente.vistaPrevia();
    componente.usarDiseno('archivo inválido');
    expect(componente.vistaPrevia()).toBe(vista); expect(componente.error()).toContain('No se pudo');
  });

  it('incluye un diseño simple aunque se hayan borrado todos los tipos', () => {
    const { fixture, componente } = crear(); fixture.componentRef.setInput('disenos', []); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('option').length).toBe(2);
    expect(ajustarDisenoMarco(disenoMarcoInicial, ajustesDisenoIniciales)).toContain('<rect');
    expect(componente.vistaPrevia()).toBeTruthy();
  });
  it('empieza desde cero con lienzo vacío y bloquea el guardado hasta agregar una parte', () => {
    const { componente, fixture, servicio } = crear(); const emitir = vi.spyOn(componente.disenoChange, 'emit');
    componente.elegir('nuevo'); fixture.detectChanges();
    expect(componente.piezas).toEqual([]); expect(emitir).toHaveBeenLastCalledWith('');
    expect(fixture.nativeElement.textContent).toContain('El lienzo está vacío');
    expect(servicio.obtenerDiseno).not.toHaveBeenCalled();
    componente.agregarPieza('lentes'); componente.agregarPieza('puente'); componente.agregarPieza('patillas');
    const svg = emitir.mock.calls.at(-1)![0]!;
    const dibujo = new DOMParser().parseFromString(svg, 'image/svg+xml');
    expect(dibujo.querySelectorAll('rect').length).toBe(2); expect(dibujo.querySelectorAll('path').length).toBe(2);
    componente.nuevo.forma = 'ovalada'; componente.actualizar();
    expect(emitir.mock.calls.at(-1)![0]).toContain('<ellipse');
    componente.quitarPieza('lentes'); componente.quitarPieza('puente'); componente.quitarPieza('patillas');
    expect(emitir).toHaveBeenLastCalledWith('');
  });

  it('descarta una plantilla pendiente al comenzar desde cero y no duplica partes', () => {
    const { componente, servicio } = crear(); const pendiente = new Subject<string>();
    servicio.obtenerDiseno.mockReturnValue(pendiente); componente.elegir('51'); componente.elegir('nuevo');
    pendiente.next(diseno); expect(componente.elegido).toBe('nuevo'); expect(componente.piezas).toEqual([]);
    componente.agregarPieza('lentes'); componente.agregarPieza('lentes'); expect(componente.piezas).toEqual(['lentes']);
  });

  it('puede importar una ilustración después de iniciar desde cero sin reconstruirla', () => {
    const { componente } = crear(); const emitir = vi.spyOn(componente.disenoChange, 'emit');
    componente.elegir('nuevo'); componente.usarDiseno(diseno); componente.actualizar();
    expect(componente.elegido).toBe('archivo'); expect(emitir.mock.calls.at(-1)![0]).toContain('<polygon');
  });

  it('ofrece todas las formas y genera contornos distintos conservando las piezas', () => {
    const { componente, fixture } = crear(); componente.elegir('nuevo'); componente.agregarPieza('lentes'); fixture.detectChanges();
    const selector = fixture.nativeElement.querySelectorAll('select')[1] as HTMLSelectElement;
    expect(selector.options.length).toBe(formasLentes.length);
    const dibujos = new Set<string>();
    for (const forma of formasLentes) {
      const svg = generarMarcoDesdeCero({ ...configuracionDesdeCero, forma: forma.id }, ['lentes', 'puente', 'patillas']);
      const documento = new DOMParser().parseFromString(svg, 'image/svg+xml');
      expect(documento.querySelector('parsererror')).toBeNull(); expect(svg).not.toContain('NaN');
      expect(documento.querySelector('g')!.children.length).toBe(4); dibujos.add(svg);
      if (forma.contorno) {
        const lentes = documento.querySelectorAll('path[vector-effect]');
        expect(lentes.length).toBe(2); expect(lentes[1].getAttribute('transform')).toContain('scale(-1 1)');
      }
    }
    expect(dibujos.size).toBe(formasLentes.length);
  });

  it('mantiene círculos y cuadrados proporcionados cuando las medidas son distintas', () => {
    const redondo = new DOMParser().parseFromString(generarMarcoDesdeCero({ ...configuracionDesdeCero, forma: 'redonda' }, ['lentes']), 'image/svg+xml');
    expect(redondo.querySelectorAll('circle').length).toBe(2);
    const cuadrado = new DOMParser().parseFromString(generarMarcoDesdeCero({ ...configuracionDesdeCero, forma: 'cuadrada' }, ['lentes']), 'image/svg+xml').querySelector('rect')!;
    expect(cuadrado.getAttribute('width')).toBe(cuadrado.getAttribute('height'));
  });

});
