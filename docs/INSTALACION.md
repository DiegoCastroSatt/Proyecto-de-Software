# Instalación, configuración y ejecución

## Requisitos

Docker Desktop y Docker Compose v2 son suficientes para levantar la plataforma principal. Para desarrollo sin Docker: Node.js 22/npm para Angular y .NET SDK 10 para ASP.NET Core. Los comandos de raíz deben ejecutarse desde el repositorio.

## Compose principal

`docker-compose.yml` define tres servicios en la red predeterminada de Compose:

| Servicio | Función | Puerto del equipo | Persistencia |
|---|---|---|---|
| `database` | MySQL 8.0; crea `optica_db` e inicializa `database/schema.sql` | `3307` → 3306 | volumen `optica_mysql_data` |
| `backend` | API .NET; espera el healthcheck de MySQL | `8080` → 8080 | `Optica.Api/wwwroot/uploads` y volumen `optica_data_protection_keys` |
| `frontend` | Compila y sirve Angular SSR en producción | `4200` → 4000 | sin volumen declarado |

El contenedor `database` declara healthcheck con `mysqladmin ping`. El frontend tiene healthcheck HTTP. La API usa la cadena de conexión construida dentro de Compose; no se debe copiar a documentación ni compartir valores secretos.

### Configuración

```bash
cp .env.example .env
```

Asigna en `.env` un `MYSQL_ROOT_PASSWORD` local. Los valores SMTP (`SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM`, `SMTP_FROM_NAME`, `SMTP_USE_TLS`) configuran correo saliente si se desea habilitar. `EMAIL_API_BASE_URL` y `EMAIL_FRONTEND_BASE_URL` son las URLs que se incorporan a enlaces enviados; los valores localhost solo sirven para abrirlos desde el mismo equipo. Mantén credenciales fuera de Git y nunca compartas `.env`.

### Comandos

```bash
docker compose config --quiet
docker compose build
docker compose up -d
docker compose ps
docker compose logs -f
docker compose logs -f backend
docker compose restart backend
docker compose stop
docker compose down
```

`down` elimina contenedores y red del proyecto, pero mantiene el volumen nombrado de MySQL. `stop` deja los contenedores. `down -v` borra volúmenes y puede eliminar datos persistentes: no usar como procedimiento rutinario. El script SQL de inicialización se ejecuta por MySQL solo al crear un directorio de datos vacío; cambiarlo no actualiza automáticamente la base ya inicializada.

El repositorio no contiene un procedimiento de migración automática asociado al arranque. Hay una migración SQL manual listada en [Base de datos](BASE_DE_DATOS.md); no la ejecutes a ciegas sobre una base existente.

### Ejecución de frontend/API por separado

Angular: en `Optica_front`, `npm ci` y `npm start` (normalmente `http://localhost:4200`). API: configura `ConnectionStrings__OpticaDb` como variable de entorno y ejecuta `dotnet run --project Optica.Api/Optica.Api.csproj`; los perfiles locales definen HTTP `5107` y HTTPS `7002`/HTTP `5107`. La API no incluye un secreto de conexión versionado. Comprueba la configuración de MySQL antes de arrancarla.

Swagger está registrado en `Program.cs` sin condición de entorno y la interfaz se sirve en `/swagger`; por lo tanto, queda disponible en cada entorno donde arranque esa API, salvo restricciones externas.

## Compose E2E aislado

`docker-compose.e2e.yml` arranca `database`, `mailpit`, `backend` y `frontend` en la red `optica-e2e-network`. MySQL usa `tmpfs` en vez de volumen persistente e inicializa el esquema más `database/e2e/02-datos-sinteticos.sql`. Mailpit captura SMTP de pruebas sin enviar a un servidor externo. El backend tiene `ASPNETCORE_ENVIRONMENT=E2E`; su healthcheck consulta `/health/db`, ruta habilitada solo en ese ambiente. El SMTP apunta a `mailpit:1025`, sin TLS ni credenciales. Los puertos publicados solo se enlazan a loopback: frontend 4200, API 8080, Mailpit SMTP 1025 e interfaz/API 8025.

Genera una contraseña temporal solo para el servicio MySQL E2E y expórtala en la terminal. En Git Bash:

```bash
export E2E_MYSQL_ROOT_PASSWORD="$(openssl rand -hex 24)"
docker compose -f docker-compose.e2e.yml config --quiet
docker compose -f docker-compose.e2e.yml up --build --force-recreate --remove-orphans --detach --wait --wait-timeout 180
cd Optica_front && npm ci && npx playwright install chromium && npm run e2e
cd ..
docker compose -f docker-compose.e2e.yml logs --no-color
docker compose -f docker-compose.e2e.yml down --remove-orphans
unset E2E_MYSQL_ROOT_PASSWORD
```

En runners Ubuntu, GitHub Actions instala dependencias Linux con `npx playwright install --with-deps chromium`; para uso local (incluido Windows/Git Bash) usa `npx playwright install chromium` como en el comando anterior. Para la secuencia completa revisa `E2E.md`. Los datos MySQL E2E desaparecen con el contenedor porque viven en memoria; `down` no requiere `-v`. No conectes esta configuración a la base principal ni al servidor desplegado. E2E usa 4200/8080 y entra en conflicto con los mismos puertos del Compose principal si ambos están activos simultáneamente.
