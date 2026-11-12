import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: 'admin/**', renderMode: RenderMode.Client },
  { path: 'cliente/inicio', renderMode: RenderMode.Client },
  { path: 'cliente/perfil', renderMode: RenderMode.Client },
  { path: 'cliente/en-trabajo', renderMode: RenderMode.Client },
  // El token de la encuesta llega en la URL; no tiene sentido prerenderizarla.
  { path: 'encuesta', renderMode: RenderMode.Client },
  {
    path: 'admin/panel/pedido/:id',
    renderMode: RenderMode.Client
  },
  {
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
