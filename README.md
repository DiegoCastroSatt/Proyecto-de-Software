# Centro Óptico San Francisco

Aplicación web para apoyar la operación de una óptica: administración de clientes, recetas, productos e inventario, ventas, pedidos y agenda de reservas. Busca centralizar información que facilita la atención y el seguimiento de productos y reservas.

## Arquitectura y tecnologías

- **Frontend:** Angular 21, TypeScript y SSR de Angular.
- **API:** ASP.NET Core sobre .NET 10, Entity Framework Core y proveedor MySQL.
- **Persistencia:** MySQL 8.0.
- **Pruebas:** Vitest (Angular), xUnit (.NET) y Playwright (E2E).
- **Entorno local:** Docker Compose. El Compose principal publica la interfaz, API y base de datos.

El frontend consume la API REST. La API aplica autenticación por cookies para personal y clientes y persiste sus datos en MySQL. La aplicación está organizada por módulos en `Optica.Api/Modules`; Angular vive en `Optica_front/`.

## Funcionalidades encontradas

Incluye gestión administrativa de clientes, recetas y graduaciones, productos/stock, catálogos, tipos de marcos y rostros, pedidos, ventas, agenda y horarios; portal y perfil de cliente, consulta de recetas propias, reservas públicas, reseñas, recomendación y probador de marcos. Existe interfaz de consulta de pedidos para clientes, pero la API de pedidos exige el permiso administrativo `pedidos`; su flujo de cliente no está completo en el estado actual. El acceso administrativo depende del permiso asignado al personal. Hay rutas explícitas de «en trabajo» y no se deben considerar funciones terminadas. El alcance y las restricciones están en [Seguridad y permisos](docs/SEGURIDAD_Y_PERMISOS.md) y [Pruebas](docs/PRUEBAS.md).

## Estructura

```text
Optica.Api/                 API ASP.NET Core
Optica_front/               aplicación Angular y pruebas Playwright
tests/                      pruebas .NET
database/schema.sql         esquema MySQL inicial
database/insert.sql         datos de ejemplo para inicialización
database/e2e/               seed sintético del entorno E2E
docs/                       documentación técnica
docker-compose.yml          entorno local principal
docker-compose.e2e.yml      entorno temporal E2E
E2E.md                      guía operativa E2E
```

## Requisitos

- Docker Desktop con Docker Compose v2 para ejecutar la plataforma completa.
- Git para clonar el repositorio.
- Node.js 22 y npm para pruebas/frontend local, y .NET SDK 10 para la API y pruebas .NET.

## Instalación rápida

Clona el repositorio, crea `.env` a partir de `.env.example`, asigna localmente `MYSQL_ROOT_PASSWORD` y ejecuta:

```bash
docker compose up --build -d
docker compose ps
```

La aplicación queda en `http://localhost:4200`, la API en `http://localhost:8080` y MySQL en `localhost:3307`. No publiques ni compartas `.env`. El volumen MySQL es persistente; `docker compose down` detiene servicios y conserva los datos. Consulta [Instalación y ejecución](docs/INSTALACION.md) para SMTP, logs y desarrollo individual.

## Pruebas

```bash
dotnet test tests/Optica_test/Optica.Tests.csproj --configuration Release
cd Optica_front
npm ci
npm test -- --watch=false
npm run e2e
```

Playwright requiere el entorno aislado descrito en [E2E.md](E2E.md); no conecta con la base principal. Consulta [Pruebas](docs/PRUEBAS.md) para preparación, artefactos y alcance real.

## Despliegue

El repositorio contiene configuración para construir/ejecutar el Compose principal, pero no identifica de forma verificable el mecanismo que actualiza el servidor externo. No se asume que pasar CI despliegue la aplicación. Ver [Despliegue](docs/DESPLIEGUE.md) para lo que sí está confirmado y los datos pendientes.

## Documentación

- [Instalación, configuración y Docker](docs/INSTALACION.md)
- [Esquema de base de datos](docs/BASE_DE_DATOS.md)
- [API y endpoints](docs/API.md)
- [Seguridad, autenticación y permisos](docs/SEGURIDAD_Y_PERMISOS.md)
- [Pruebas](docs/PRUEBAS.md)
- [CI y despliegue](docs/DESPLIEGUE.md)
- [Entorno E2E](E2E.md)
