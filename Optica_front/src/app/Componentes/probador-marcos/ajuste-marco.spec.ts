import { calcularAjuste } from './ajuste-marco';
import type { PuntoFacial } from './detector-rostro.service';

function ojos(a: PuntoFacial, b: PuntoFacial): PuntoFacial[] {
  const puntos: PuntoFacial[] = [];
  puntos[468] = a;
  puntos[473] = b;
  return puntos;
}

describe('Ajuste automático del marco', () => {
  it('centra el marco entre los ojos', () => {
    const ajuste = calcularAjuste(ojos({ x: 0.35, y: 0.4 }, { x: 0.65, y: 0.4 }), 800, 600)!;
    expect(ajuste.x).toBeCloseTo(50);
    expect(ajuste.y).toBeCloseTo(40);
    expect(ajuste.ancho).toBeCloseTo(62.0689);
    expect(ajuste.angulo).toBe(0);
  });

  it('compensa los márgenes de una foto vertical sin deformarla', () => {
    const ajuste = calcularAjuste(ojos({ x: 0.35, y: 0.4 }, { x: 0.65, y: 0.4 }), 600, 1200)!;
    expect(ajuste.x).toBeCloseTo(50);
    expect(ajuste.y).toBeCloseTo(40);
    expect(ajuste.ancho).toBeCloseTo(23.27586);
  });

  it('compensa el espacio superior de una foto panorámica', () => {
    const ajuste = calcularAjuste(ojos({ x: 0.35, y: 0.4 }, { x: 0.65, y: 0.4 }), 1200, 600)!;
    expect(ajuste.y).toBeCloseTo(43.3333);
  });

  it('inclina el marco usando las dimensiones reales de la imagen', () => {
    const ajuste = calcularAjuste(ojos({ x: 0.35, y: 0.3 }, { x: 0.65, y: 0.5 }), 800, 600)!;
    expect(ajuste.angulo).toBeCloseTo((Math.atan2(120, 240) * 180) / Math.PI);
  });

  it('descarta ojos ausentes, coordenadas inválidas y tamaños sin cargar', () => {
    expect(calcularAjuste([], 800, 600)).toBeNull();
    expect(calcularAjuste(ojos({ x: NaN, y: 0.4 }, { x: 0.65, y: 0.4 }), 800, 600)).toBeNull();
    expect(calcularAjuste(ojos({ x: 0.35, y: 0.4 }, { x: 0.65, y: 0.4 }), 0, 0)).toBeNull();
  });
});
