import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: 'admin/**', renderMode: RenderMode.Client },
  { path: 'cliente/inicio', renderMode: RenderMode.Client },
  { path: 'cliente/en-trabajo', renderMode: RenderMode.Client },
  {
    path: 'admin/panel/pedido/:id',
    renderMode: RenderMode.Client
  },
  {
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
