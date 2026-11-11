# Pruebas

## Backend

El proyecto de pruebas está en `tests/Optica_test/Optica.Tests.csproj` y usa xUnit. Ejecuta desde la raíz:

```bash
dotnet test tests/Optica_test/Optica.Tests.csproj --configuration Release
```

La suite contiene pruebas unitarias y de integración de servicios/controladores del backend (revisar nombres bajo `tests/Optica_test`). El workflow también busca proyectos `*Tests.csproj` bajo `tests` y guarda resultados TRX como artefacto.

## Frontend

Angular usa Vitest vía CLI Angular. Hay 43 archivos `*.spec.ts` en `Optica_front/src/app` al auditar. Ejecuta:

```bash
cd Optica_front
npm ci
npm test -- --watch=false
npm run build
```

Las especificaciones se distribuyen junto a componentes/servicios/guards; cubren unidades concretas, no equivalen a demostrar todos los flujos de extremo a extremo.

## Playwright E2E

Configuración: `Optica_front/playwright.config.ts`. Proyecto Chromium (Desktop Chrome), base URL `http://localhost:4200`, timeout de test 30 s, expectativas 10 s, retries 1 en CI y 0 local. Reporte HTML en `Optica_front/playwright-report/`; `test-results/` contiene salidas, capturas y trazas. Screenshot solo al fallar y trace `retain-on-failure`.

Levanta primero el entorno E2E aislado según [E2E.md](../E2E.md) o [Instalación](INSTALACION.md). Luego:

```bash
cd Optica_front
npm ci
npx playwright install chromium
npm run e2e
npm run e2e:report
```

Hay tres specs bajo `Optica_front/e2e/`: autenticación, navegación/frontend y reserva/confirmación. La spec de reserva genera una reserva pública sin sesión, espera el correo de Mailpit, utiliza el enlace real para confirmar y consulta la agenda administrativa. Los otros specs cubren sus flujos de autenticación e interfaz definidos en el código. No se declara cobertura de flujos que no tengan un spec.

Para la suite completa local: backend `dotnet test`, frontend `npm test -- --watch=false`, y E2E en el Compose separado. En GitHub Actions estos grupos se ejecutan en tres jobs (`backend`, `frontend`, `e2e`) independientes.

## Evidencias y seguridad

Playwright produce trazas/capturas por fallo. GitHub Actions carga logs de Compose y evidencias E2E solo si hay fallo, tras pasar el script `.github/scripts/sanitize-e2e-artifacts.py`; el artefacto expira a 14 días. La redacción es una defensa adicional; antes de adjuntar evidencias manualmente, inspecciónalas y no incluyas secretos/datos personales.
