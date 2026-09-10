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
- Node.js y npm para ejecutar el frontend Angular.

### Backend y base de datos

1. Clonar el repositorio y entrar a su carpeta:

```powershell
git clone <URL_DEL_REPOSITORIO>
cd Proyecto_de_Software
```

2. Crear el archivo `.env` a partir de `.env.example` y reemplazar las claves de ejemplo por contraseñas locales robustas. Este archivo está ignorado por Git y no debe compartirse:

```powershell
Copy-Item .env.example .env
```

La API utiliza `MYSQL_ROOT_PASSWORD`, definido solo en el archivo local `.env`. No se deben versionar contraseñas reales, ni siquiera en archivos de configuración de la API.

3. Construir y levantar la API junto con MySQL:

```powershell
docker compose up --build -d
```

La API quedara disponible en `http://localhost:8080` y MySQL en `localhost:3307`.

Para ver los logs:

```powershell
docker compose logs -f
```

Para detener los servicios:

```powershell
docker compose down
```

El volumen de MySQL conserva los datos. Para eliminar también esos datos y volver a crear la base desde cero, usar:

```powershell
docker compose down -v
docker compose up --build -d
```

`database/schema.sql` es autocontenido: crea la base `optica_db`, todas las tablas, sus relaciones, restricciones e índices. Docker lo ejecuta automáticamente cuando el volumen está vacío.

> Si una contraseña fue usada en un commit anterior, debe rotarse. Eliminarla de la versión actual no la elimina del historial de Git; para borrarla de este se requiere una reescritura coordinada del historial remoto.

### Frontend Angular

En otra terminal:

```powershell
cd Optica_front
npm install
npm start
```

El frontend se abrira normalmente en `http://localhost:4200`.
