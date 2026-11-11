# CI y despliegue

## GitHub Actions verificado

El workflow `.github/workflows/tests.yml` se activa con `pull_request` hacia `main`, `push` a `main` y `workflow_dispatch`. Define tres jobs independientes en Ubuntu:

- **API y pruebas .NET:** SDK 10, build Release y proyectos `*Tests.csproj` encontrados bajo `tests`; carga TRX.
- **Pruebas y compilación Angular:** Node 22, `npm ci`, `npm test -- --watch=false` y build.
- **E2E Playwright:** Node 22/Chromium, genera secreto temporal en el runner, construye y levanta solo `docker-compose.e2e.yml`, ejecuta E2E, recoge logs/evidencias tras fallos con sanitización y retira el Compose E2E.

El job E2E emplea MySQL temporal con `tmpfs` y Mailpit; no debe configurarse para apuntar a la base del servidor. CI informa resultados y artefactos en la ejecución del workflow. Para impedir merge cuando falla, la configuración de protección de rama/ruleset en GitHub debe exigir los checks; no está versionada en este repositorio y no se puede confirmar su estado.

Un PR aprobado por esos jobs solo significa que pasó las comprobaciones configuradas. El push/merge a `main` también ejecuta workflow, pero este YAML no contiene un job de publicación/despliegue. La ejecución de CI, la integración en `main` y el despliegue son sucesos distintos.

## Servidor externo

La solicitud del proyecto indica que existe un servidor externo conectado al repositorio y que utiliza el Docker principal. En el repositorio inspeccionado no hay workflow de deploy, configuración de proveedor/cloud, dirección, unidad de servicio, script de actualización, rama desplegada ni instrucciones operativas. `docker-compose.yml` describe los servicios locales y por sí mismo no demuestra cómo el servidor obtiene cambios ni cómo se activa un despliegue.

Por tanto, el procedimiento remoto, comandos de acceso, proveedor, rama y gestión remota de datos/secretos quedan **sin verificar** y no se documenta un procedimiento supuesto. Para completar esta sección se necesita que el equipo confirme: proveedor/plataforma y host/documentación de acceso; evento y método que inicia el despliegue; rama/tag desplegado; ubicación/operación de Compose; gestión de variables secretas y SMTP; estrategia de backups y cambios de esquema; forma soportada de ver logs y comprobar disponibilidad. No incluir credenciales en este documento.

No se realizaron conexiones, reinicios ni despliegues al servidor.
