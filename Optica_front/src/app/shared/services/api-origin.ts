/** Devuelve el origen de la API según el origen desde el que se abrió el frontend. */
export function obtenerOrigenApi(): string {
  if (typeof window !== 'undefined') {
    const { hostname, protocol } = window.location;
    const sufijoTunnelFrontend = '-4200.brs.devtunnels.ms';

    if (hostname.endsWith(sufijoTunnelFrontend)) {
      const identificadorTunnel = hostname.slice(0, -sufijoTunnelFrontend.length);
      return `${protocol}//${identificadorTunnel}-8080.brs.devtunnels.ms`;
    }
  }

  return 'http://localhost:8080';
}
