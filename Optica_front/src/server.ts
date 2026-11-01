import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import type { OutgoingHttpHeaders } from 'node:http';
import { request as httpsRequest } from 'node:https';
import { join } from 'node:path';

const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
const apiProxyTarget = process.env['API_PROXY_TARGET'] ?? 'https://proyecto-de-software-0ghx.onrender.com';
const hostRender = process.env['RENDER_EXTERNAL_HOSTNAME'];
const hostsPermitidos = ['localhost', '127.0.0.1', ...(hostRender ? [hostRender] : [])];
const angularApp = new AngularNodeAppEngine({
  allowedHosts: hostsPermitidos,
  trustProxyHeaders: true,
});

/**
 * Example Express Rest API endpoints can be defined here.
 * Uncomment and define endpoints as necessary.
 *
 * Example:
 * ```ts
 * app.get('/api/{*splat}', (req, res) => {
 *   // Handle API request
 * });
 * ```
 */

/** Mantiene las llamadas y cookies de sesión bajo el mismo dominio del frontend. */
app.use(['/api', '/uploads'], (req, res) => {
  const destino = new URL(req.originalUrl, apiProxyTarget);
  const encabezados: OutgoingHttpHeaders = { ...req.headers, host: destino.host };
  const encabezadosSalto = new Set([
    'connection', 'keep-alive', 'proxy-authenticate', 'proxy-authorization',
    'te', 'trailer', 'transfer-encoding', 'upgrade',
  ]);
  for (const encabezado of encabezadosSalto) delete encabezados[encabezado];

  const solicitudApi = httpsRequest(destino, {
    method: req.method,
    headers: encabezados,
  }, respuestaApi => {
    res.statusCode = respuestaApi.statusCode ?? 502;
    for (const [nombre, valor] of Object.entries(respuestaApi.headers)) {
      if (valor !== undefined && !encabezadosSalto.has(nombre.toLowerCase()))
        res.setHeader(nombre, valor);
    }
    respuestaApi.pipe(res);
  });

  solicitudApi.on('error', error => {
    console.error(`Falló la conexión del proxy con la API (${error.name}).`);
    if (!res.headersSent)
      res.status(502).json({ mensaje: 'No se pudo conectar con la API.' });
    else
      res.destroy(error);
  });

  req.on('aborted', () => solicitudApi.destroy());
  req.pipe(solicitudApi);
});

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 */
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) =>
      response ? writeResponseToNodeResponse(response, res) : next(),
    )
    .catch(next);
});

/**
 * Start the server if this module is the main entry point, or it is ran via PM2.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, (error) => {
    if (error) {
      throw error;
    }

    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
