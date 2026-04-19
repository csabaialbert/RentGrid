import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { join } from 'node:path';

const browserDistFolder = join(import.meta.dirname, '../browser');
const apiServerUrl = process.env['API_URL'] || 'http://localhost:5001';

const app = express();
const angularApp = new AngularNodeAppEngine();

app.use('/api', async (req, res, next) => {
  try {
    // Itt NEM vágjuk le az /api-t, mert a kontrollered route-ja [Route("api/[controller]")]
    const targetUrl = new URL(req.originalUrl, apiServerUrl);

    console.log(`Proxying request to: ${targetUrl.href}`); // Segít a hibakeresésben

    const headers = new Headers();
    Object.entries(req.headers).forEach(([key, value]) => {
      // A 'host' fejlécet tilos átvinni, a többit mehet
      if (value && key.toLowerCase() !== 'host') {
        headers.append(key, Array.isArray(value) ? value.join(',') : (value as string));
      }
    });

    const response = await fetch(targetUrl.href, {
      method: req.method,
      headers: headers,
      // A (req as any) használatával elhallgattatjuk a TypeScript hibát
      body: ['GET', 'HEAD'].includes(req.method) ? undefined : (req as any),
      // @ts-ignore
      duplex: 'half', 
    });

    res.status(response.status);
    response.headers.forEach((value, key) => {
      if (key.toLowerCase() !== 'transfer-encoding') {
        res.setHeader(key, value);
      }
    });

    const arrayBuffer = await response.arrayBuffer();
    res.send(Buffer.from(arrayBuffer));
  } catch (error) {
    console.error('Proxy hiba történt:', error);
    next(error);
  }
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
