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

2. Crear el archivo `.env` a partir de `.env.example`:

```powershell
Copy-Item .env.example .env
```

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

El volumen de MySQL conserva los datos. Para eliminar tambien esos datos, usar `docker compose down -v`.

### Frontend Angular

En otra terminal:

```powershell
cd Optica_front
npm install
npm start
```

El frontend se abrira normalmente en `http://localhost:4200`.