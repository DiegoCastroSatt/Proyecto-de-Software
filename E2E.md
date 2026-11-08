# Entorno aislado para pruebas E2E

Este entorno utiliza `docker-compose.e2e.yml`, un proyecto y una red propios, y un contenedor MySQL sin volumen persistente. No comparte la base ni los volúmenes del Compose habitual.

## Requisitos e instalación

- Docker Desktop con el motor Linux iniciado.
- Node.js 22, la versión usada por GitHub Actions, y npm 11.12.1, indicado por `packageManager` en `Optica_front/package.json`.
- Puertos locales 1025 para SMTP de Mailpit, 8025 para su interfaz, 8080 para la API y 4200 para el frontend.

Desde la raíz del repositorio, instala las dependencias del frontend y Chromium:

```bash
cd Optica_front
npm ci
npx playwright install chromium
cd ..
```

En runners Ubuntu también se pueden instalar las dependencias del sistema requeridas por Chromium con `npx playwright install --with-deps chromium`.

## Levantar el entorno E2E

Desde la raíz del repositorio, en Git Bash, crea una contraseña aleatoria solo para esta sesión e inicia el Compose aislado:

```bash
export E2E_MYSQL_ROOT_PASSWORD="$(openssl rand -base64 32)"
docker compose -f docker-compose.e2e.yml up --build --detach --wait --wait-timeout 180
```

Mantén abierta esta sesión de Git Bash para poder detener el mismo proyecto al terminar. Compose inicia MySQL, ejecuta `database/schema.sql` y luego `database/e2e/02-datos-sinteticos.sql`. La base se llama `optica_db`, pero existe solo dentro del contenedor efímero `optica-e2e`.

## Servicios y navegador

- Frontend: <http://localhost:4200>
- API: <http://localhost:8080>
- Mailpit web/API: <http://localhost:8025>
- SMTP de Mailpit dentro de la red Compose: `mailpit:1025`

Las imágenes de MySQL y Mailpit están fijadas por digest para evitar cambios inesperados de imagen entre ejecuciones.

Angular usa URLs de API basadas en `http://localhost:8080`. En CI, ejecutar Playwright en el propio runner (con los navegadores instalados allí), no en otro contenedor: desde el runner, `localhost` llega a los puertos publicados. Enviar las solicitudes desde el navegador conserva el origen `http://localhost:4200`, permitido por la política CORS actual. Si Playwright corre en un contenedor separado, `localhost` apuntaría a ese contenedor y no a la API; habría que resolver esa topología antes de usarla.

Playwright no inicia Docker. Levanta el entorno con el comando anterior y ejecuta las pruebas desde otra terminal Git Bash, dentro de `Optica_front`:

```bash
cd Optica_front
npm run e2e
```

Para depurar con interfaz visual o modo paso a paso:

```bash
npm run e2e:ui
npm run e2e:debug
```

Las pruebas están en `Optica_front/e2e/` y usan Chromium. La prueba de portada verifica el estado HTTP, el título real `OpticaWeb` y el encabezado accesible de la página inicial. La prueba de autenticación usa la cuenta cliente sintética del seed, verifica el `POST` de login, la redirección al portal y una respuesta satisfactoria de sesión después de recargar.

## Correo y datos sintéticos

El backend apunta a Mailpit con `Correo__SmtpHost=mailpit`, `Correo__Puerto=1025` y TLS desactivado. Las credenciales SMTP están vacías; no se configura ningún servidor externo. La cola de reservas continúa persistida en MySQL y el procesador de fondo la consulta aproximadamente cada minuto. Mailpit permite consultar los mensajes en la interfaz web.

El SQL E2E crea un administrador ficticio requerido por la clave foránea de `horarios_atencion`, una cuenta cliente ficticia activa en `clientes`/`cuentas_cliente` y un horario futuro. El hash de la contraseña se generó con el `PasswordHasher<CuentaCliente>` de ASP.NET Core que utiliza el backend. No contiene clientes reales ni datos de `insert.sql`; la reserva E2E puede generar su propio cliente sintético por el flujo público.

## Comprobaciones y detención

El Compose define comprobaciones de salud para MySQL, API y frontend. En el entorno `E2E`, la API expone `/health/db`, que usa `Database.CanConnectAsync()` y solo responde con disponibilidad, sin devolver datos de clientes. El endpoint existente `/test-db` requiere autenticación y no se usa como healthcheck. La inicialización se ejecuta cuando MySQL encuentra su directorio de datos vacío; en este entorno el directorio está en `tmpfs` y desaparece al quitar el contenedor.

Los resultados se guardan en `Optica_front/playwright-report/` y `Optica_front/test-results/`. La traza y la captura de pantalla se conservan cuando falla una prueba. Consulta el informe HTML con:

```bash
npm run e2e:report
```

Detén y retira únicamente el proyecto E2E desde la sesión Git Bash original, que debe seguir abierta en la raíz del repositorio con la variable de contraseña definida:

```bash
docker compose -f docker-compose.e2e.yml down --remove-orphans
```

No usar `down -v`. No iniciar este Compose al mismo tiempo que el Compose de desarrollo si ambos necesitan los mismos puertos locales.
