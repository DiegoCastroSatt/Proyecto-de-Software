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
