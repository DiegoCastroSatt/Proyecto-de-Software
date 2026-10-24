export const disenoMarcoInicial = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120" fill="none"><g stroke="#222222" stroke-width="4" stroke-linejoin="round"><rect x="24" y="35" width="74" height="50" rx="12"/><rect x="142" y="35" width="74" height="50" rx="12"/><path d="M98 49Q120 38 142 49M24 44L9 40M216 44L231 40"/></g></svg>';

export interface AjustesDisenoMarco { ancho: number; alto: number; grosor: number; color: string; }
export const ajustesDisenoIniciales: AjustesDisenoMarco = { ancho: 100, alto: 100, grosor: 4, color: '#222222' };

export function ajustarDisenoMarco(svg: string, ajustes: AjustesDisenoMarco): string {
  const documento = new DOMParser().parseFromString(svg, 'image/svg+xml');
  const raiz = documento.documentElement;
  if (documento.querySelector('parsererror') || raiz.localName !== 'svg') throw new Error('Ilustración inválida');
  const vista = (raiz.getAttribute('viewBox') ?? '').trim().split(/[ ,]+/).map(Number);
  if (vista.length !== 4 || vista.some(n => !Number.isFinite(n)) || vista[2] <= 0 || vista[3] <= 0) throw new Error('Tamaño inválido');
  const ancho = Math.max(60, Math.min(115, ajustes.ancho)) / 100;
  const alto = Math.max(60, Math.min(115, ajustes.alto)) / 100;
  const grosor = Math.max(1, Math.min(8, ajustes.grosor));
  const color = /^#[0-9a-f]{6}$/i.test(ajustes.color) ? ajustes.color : '#222222';
  // Recorrer el diseño real: no reconstruir sus lentes, puente ni patillas con una lista fija.
  for (const elemento of [raiz, ...Array.from(raiz.querySelectorAll('*'))]) {
    const trazo = elemento.getAttribute('stroke');
    if (trazo && trazo !== 'none') elemento.setAttribute('stroke', color);
    const relleno = elemento.getAttribute('fill');
    if (relleno && !['none', 'transparent'].includes(relleno) && !relleno.startsWith('url(')) elemento.setAttribute('fill', color);
    const espesor = elemento.getAttribute('stroke-width');
    if (espesor && /^\d+(?:\.\d+)?(?:px)?$/.test(espesor)) elemento.setAttribute('stroke-width', String(parseFloat(espesor) * grosor / 4));
  }
  const grupo = documento.createElementNS('http://www.w3.org/2000/svg', 'g');
  const centroX = vista[0] + vista[2] / 2, centroY = vista[1] + vista[3] / 2;
  grupo.setAttribute('transform', `translate(${centroX} ${centroY}) scale(${ancho} ${alto}) translate(${-centroX} ${-centroY})`);
  while (raiz.firstChild) grupo.appendChild(raiz.firstChild);
  raiz.appendChild(grupo);
  const margen = Math.max(0, grosor - 4);
  const anchoVista = vista[2] * Math.max(1, ancho) + margen * 2;
  const altoVista = vista[3] * Math.max(1, alto) + margen * 2;
  raiz.setAttribute('viewBox', `${centroX - anchoVista / 2} ${centroY - altoVista / 2} ${anchoVista} ${altoVista}`);
  raiz.removeAttribute('width'); raiz.removeAttribute('height');
  return new XMLSerializer().serializeToString(raiz);
}

export interface FormaLenteDiseno { id: string; nombre: string; contorno: string; geometria?: string; mantenerProporcion?: boolean; }
export type FormaLente = string;

export type PiezaMarco = 'lentes' | 'puente' | 'patillas';
export interface DisenoDesdeCero {
  forma: FormaLente; ancho: number; alto: number; separacion: number;
  esquinas: number; posicion: number; curvatura: number; largoPatillas: number;
}
export const configuracionDesdeCero: DisenoDesdeCero = {
  forma: '', ancho: 74, alto: 50, separacion: 44, esquinas: 12,
  posicion: 60, curvatura: 11, largoPatillas: 15,
};
export const nombresPiezas: Record<PiezaMarco, string> = { lentes: 'Lentes', puente: 'Puente', patillas: 'Patillas' };

export function generarMarcoDesdeCero(config: DisenoDesdeCero, piezas: PiezaMarco[], formas: readonly FormaLenteDiseno[] = []): string {
  const limitar = (n: number, min: number, max: number) => Math.max(min, Math.min(max, Number.isFinite(n) ? n : min));
  let ancho = limitar(config.ancho, 32, 84), alto = limitar(config.alto, 24, 80);
  const forma = formas.find(item => item.id === config.forma);
  if (forma?.mantenerProporcion) ancho = alto = Math.min(ancho, alto);
  const separacion = limitar(config.separacion, 8, 48);
  const y = limitar(config.posicion, alto / 2 + 8, 112 - alto / 2);
  const izquierda = 120 - separacion / 2 - ancho, derecha = 120 + separacion / 2;
  const curvatura = limitar(config.curvatura, 0, 24);
  const largo = limitar(config.largoPatillas, 0, Math.min(20, izquierda - 5));
  const elementos: string[] = [];
  for (const pieza of [...new Set(piezas)]) {
    if (pieza === 'lentes' && forma) {
      for (const [lado, x] of [izquierda, derecha].entries()) {
        if (forma.geometria === 'elipse') {
          elementos.push(`<ellipse cx="${x + ancho / 2}" cy="${y}" rx="${ancho / 2}" ry="${alto / 2}"/>`);
        } else if (forma.geometria === 'rectangulo') {
          elementos.push(`<rect x="${x}" y="${y - alto / 2}" width="${ancho}" height="${alto}" rx="${limitar(config.esquinas, 0, Math.min(ancho, alto) / 2)}"/>`);
        } else if (forma.contorno) {
          // Reflejar el lente derecho para conservar la simetría del contorno guardado.
          const reflejo = lado === 1 ? ' translate(1 0) scale(-1 1)' : '';
          elementos.push(`<path d="${forma.contorno}" transform="translate(${x} ${y - alto / 2}) scale(${ancho} ${alto})${reflejo}" vector-effect="non-scaling-stroke"/>`);
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
