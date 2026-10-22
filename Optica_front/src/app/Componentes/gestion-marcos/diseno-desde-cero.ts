export const formasLentes = [
  { id: 'rectangular', nombre: 'Rectangular', contorno: '' },
  { id: 'ovalada', nombre: 'Ovalada', contorno: '' },
  { id: 'redonda', nombre: 'Redonda', contorno: '' },
  { id: 'cuadrada', nombre: 'Cuadrada', contorno: '' },
  { id: 'aviador', nombre: 'Aviador', contorno: 'M.04 .12 Q.2 .02 .42 .08 L.88 .17 Q1 .2 1 .28 Q1 .55 .75 .86 Q.58 1 .38 .96 Q.1 .91 .03 .6 Q0 .35 .04 .12Z' },
  { id: 'cat-eye', nombre: 'Cat eye', contorno: 'M.02 0 Q.4 .2 .85 .15 Q1 .14 1 .28 L.9 .68 Q.8 .95 .45 .9 Q.1 .84 .03 .48 L0 .28Z' },
  { id: 'wayfarer', nombre: 'Wayfarer', contorno: 'M.05 .05 Q.4 0 .88 .08 Q1 .1 1 .28 L.85 .87 Q.82 .98 .64 1 H.28 Q.1 .98 .08 .8 L0 .28Q0 .1 .05 .05Z' },
  { id: 'hexagonal', nombre: 'Hexagonal', contorno: 'M.18 0 H.82 L1 .28 L.82 1 H.18 L0 .28Z' },
  { id: 'octagonal', nombre: 'Octagonal', contorno: 'M.2 0 H.8 L1 .28 V.72 L.8 1 H.2 L0 .72 V.28Z' },
  { id: 'mariposa', nombre: 'Mariposa', contorno: 'M.04 0 Q.45 .03 .9 .2 Q1 .22 1 .28 Q1 .6 .8 .86 Q.65 1 .35 .96 Q.08 .9 .02 .55 L0 .28Z' },
] as const;
export type FormaLente = typeof formasLentes[number]['id'];

export type PiezaMarco = 'lentes' | 'puente' | 'patillas';
export interface DisenoDesdeCero {
  forma: FormaLente; ancho: number; alto: number; separacion: number;
  esquinas: number; posicion: number; curvatura: number; largoPatillas: number;
}
export const configuracionDesdeCero: DisenoDesdeCero = {
  forma: 'rectangular', ancho: 74, alto: 50, separacion: 44, esquinas: 12,
  posicion: 60, curvatura: 11, largoPatillas: 15,
};
export const nombresPiezas: Record<PiezaMarco, string> = { lentes: 'Lentes', puente: 'Puente', patillas: 'Patillas' };

export function generarMarcoDesdeCero(config: DisenoDesdeCero, piezas: PiezaMarco[]): string {
  const limitar = (n: number, min: number, max: number) => Math.max(min, Math.min(max, Number.isFinite(n) ? n : min));
  let ancho = limitar(config.ancho, 32, 84), alto = limitar(config.alto, 24, 80);
  if (config.forma === 'redonda' || config.forma === 'cuadrada') ancho = alto = Math.min(ancho, alto);
  const separacion = limitar(config.separacion, 8, 48);
  const y = limitar(config.posicion, alto / 2 + 8, 112 - alto / 2);
  const izquierda = 120 - separacion / 2 - ancho, derecha = 120 + separacion / 2;
  const curvatura = limitar(config.curvatura, 0, 24);
  const largo = limitar(config.largoPatillas, 0, Math.min(20, izquierda - 5));
  const elementos: string[] = [];
  for (const pieza of [...new Set(piezas)]) {
    if (pieza === 'lentes') {
      for (const [lado, x] of [izquierda, derecha].entries()) {
        if (config.forma === 'ovalada') {
          elementos.push(`<ellipse cx="${x + ancho / 2}" cy="${y}" rx="${ancho / 2}" ry="${alto / 2}"/>`);
        } else if (config.forma === 'redonda') {
          elementos.push(`<circle cx="${x + ancho / 2}" cy="${y}" r="${ancho / 2}"/>`);
        } else {
          const contorno = formasLentes.find(forma => forma.id === config.forma)?.contorno;
          if (contorno) {
            // Reflejar el lente derecho para conservar la simetría de las formas asimétricas.
            const reflejo = lado === 1 ? ' translate(1 0) scale(-1 1)' : '';
            elementos.push(`<path d="${contorno}" transform="translate(${x} ${y - alto / 2}) scale(${ancho} ${alto})${reflejo}" vector-effect="non-scaling-stroke"/>`);
          } else {
            elementos.push(`<rect x="${x}" y="${y - alto / 2}" width="${ancho}" height="${alto}" rx="${limitar(config.esquinas, 0, Math.min(ancho, alto) / 2)}"/>`);
          }
        }
      }
    }
    if (pieza === 'puente') {
      const altura = y - alto * .22;
      elementos.push(`<path d="M${120 - separacion / 2} ${altura} Q120 ${altura - curvatura} ${120 + separacion / 2} ${altura}"/>`);
    }
    if (pieza === 'patillas') {
      const altura = y - alto * .32;
      elementos.push(`<path d="M${izquierda} ${altura} L${izquierda - largo} ${altura - 4} M${derecha + ancho} ${altura} L${derecha + ancho + largo} ${altura - 4}"/>`);
    }
  }
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g stroke="#222222" stroke-width="4" stroke-linecap="round" stroke-linejoin="round">${elementos.join('')}</g></svg>`;
}
