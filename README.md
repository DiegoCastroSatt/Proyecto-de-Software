# Proyecto-de-Software

Equipo 1

Integrantes:
1. Diego Castro
2. Diego Peña
3. Claudio Parra
4. Alejandro Santibañez
5. Tomás Marambio
6. Ricardo Barreto

*Descripción:*
Nosotros trabajaremos con el Centro óptico San Francisco, creando una aplicación web para solucionar ciertos problemas/deficits que posee,como por ejemplo dificultades a la hora de notificar el estado de los productos a los clientes. El equipo desarrollara una plataforma que permita el seguimiento de las prescripciones, visualizacion y modificacion de stock en el inventario y otros datos. 

*Stack:* Para el Backend .NET, para el Frontend es Angular y la base de datos es MySQL.

## Ejecucion con Docker

### Requisitos

- Docker Desktop instalado y ejecutandose.
- Git para clonar el repositorio.
- Node.js y npm solo si se ejecuta Angular fuera de Docker.

### Plataforma completa

1. Clonar el repositorio y entrar a su carpeta:

```bash
git clone <URL_DEL_REPOSITORIO>
cd Proyecto_de_Software
```

2. Crear el archivo `.env` a partir de `.env.example` y configurar `MYSQL_ROOT_PASSWORD`.

```bash
Copy-Item .env.example .env
```

3. Configurar `MYSQL_ROOT_PASSWORD` en `.env` y construir y levantar toda la plataforma:

```bash
docker compose up --build -d
```

Angular quedará disponible en `http://localhost:4200`, la API en `http://localhost:8080` y MySQL en `localhost:3307`. No necesitas ejecutar `npm start` para usar Angular con Docker. Si hay un servidor local de Angular usando el puerto 4200, detenlo antes de iniciar Compose.

El contenedor del frontend compila Angular y ejecuta su servidor de producción. La API espera a que MySQL esté disponible y los datos se conservan en el volumen existente.

Esta configuración está preparada para abrir la plataforma desde el mismo equipo: el navegador consulta la API en `localhost:8080`.

Para el uso local, `EMAIL_API_BASE_URL=http://localhost:8080` permite enviar los correos; sus enlaces funcionan al abrirlos en el mismo equipo donde corre Docker. Si el cliente abre el mensaje desde otro dispositivo, configura `EMAIL_API_BASE_URL` en `.env` con la URL HTTPS pública de la API, porque `localhost` apunta al dispositivo que abre el enlace.

Para ver los logs:

```bash
docker compose logs -f
```

Para detener los servicios:

```bash
docker compose down
```

El volumen de MySQL conserva los datos. Para eliminar tambien esos datos, usar `docker compose down -v`.

### Frontend Angular para desarrollo sin Docker

Para trabajar en Angular con recarga automática, ejecuta en otra terminal (sin el servicio `frontend` de Compose activo):

```bash
cd Optica_front
npm install
npm start
```

El frontend se abrira normalmente en `http://localhost:4200`.

### Autenticación y actualización de una base existente

El formulario unificado está disponible en `/cliente/login` y `/admin`. Los clientes ingresan con RUT válido o correo; administradores y trabajadores, con su nombre de usuario. La recuperación pide RUT para clientes y nombre de usuario para personal, y envía un código de seis dígitos al correo registrado.

Los códigos de recuperación se conservan en memoria por cinco minutos. Si la API se reinicia, los códigos pendientes dejan de ser válidos y se puede solicitar uno nuevo.

Los datos de ejemplo están en `database/insert.sql`.
