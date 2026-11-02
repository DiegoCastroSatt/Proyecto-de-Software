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

  function rostro(izquierda: PuntoFacial, derecha: PuntoFacial): PuntoFacial[] {
    const puntos = ojos({ x: 0.35, y: 0.4 }, { x: 0.65, y: 0.4 });
    puntos[234] = izquierda;
    puntos[454] = derecha;
    return puntos;
  }

  it('adapta el ancho a caras distintas aunque tengan la misma distancia entre ojos', () => {
    const estrecha = calcularAjuste(rostro({ x: 0.25, y: 0.5 }, { x: 0.75, y: 0.5 }), 800, 600)!;
    const ancha = calcularAjuste(rostro({ x: 0.15, y: 0.5 }, { x: 0.85, y: 0.5 }), 800, 600)!;
    expect(estrecha.ancho).toBeCloseTo(50);
    expect(ancha.ancho).toBeCloseTo(70);
    expect(ancha.x).toBeCloseTo(estrecha.x);
    expect(ancha.y).toBeCloseTo(estrecha.y);
  });

  it('mantiene la proporción del rostro en fotos verticales', () => {
    const ajuste = calcularAjuste(rostro({ x: 0.2, y: 0.5 }, { x: 0.8, y: 0.5 }), 600, 1200)!;
    expect(ajuste.ancho).toBeCloseTo(22.5);
  });

  it('mide la cara inclinada con las dimensiones reales de la imagen', () => {
    const puntos = rostro({ x: 0.2, y: 0.3 }, { x: 0.8, y: 0.7 });
    puntos[468] = { x: 0.35, y: 0.4 };
    puntos[473] = { x: 0.65, y: 0.6 };
    const ajuste = calcularAjuste(puntos, 800, 600)!;
    expect(ajuste.ancho).toBeCloseTo(Math.hypot(480, 240) / 800 * 100);
    expect(ajuste.angulo).toBeCloseTo(Math.atan2(120, 240) * 180 / Math.PI);
  });

  it('usa los ojos si el contorno es inválido o demasiado pequeño', () => {
    const esperado = calcularAjuste(ojos({ x: 0.35, y: 0.4 }, { x: 0.65, y: 0.4 }), 800, 600)!;
    for (const lateral of [{ x: NaN, y: 0.5 }, { x: -0.1, y: 0.5 }, { x: 0.79, y: 0.5 }]) {
      const ajuste = calcularAjuste(rostro(lateral, { x: 0.8, y: 0.5 }), 800, 600)!;
      expect(ajuste.ancho).toBeCloseTo(esperado.ancho);
    }
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
