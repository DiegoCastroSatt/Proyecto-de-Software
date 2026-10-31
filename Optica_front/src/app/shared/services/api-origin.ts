/** Devuelve el origen de la API según el origen desde el que se abrió el frontend. */
export function obtenerOrigenApi(): string {
  if (typeof window !== 'undefined') {
    const { hostname, protocol } = window.location;
    const sufijoTunnelFrontend = '-4200.brs.devtunnels.ms';

    if (hostname.endsWith(sufijoTunnelFrontend)) {
      const identificadorTunnel = hostname.slice(0, -sufijoTunnelFrontend.length);
      return `${protocol}//${identificadorTunnel}-8080.brs.devtunnels.ms`;
    }

    if (hostname !== 'localhost' && hostname !== '127.0.0.1') {
      return 'https://proyecto-de-software-0ghx.onrender.com';
    }
  }

  return typeof window === 'undefined'
    ? 'https://proyecto-de-software-0ghx.onrender.com'
    : 'http://localhost:8080';
}
