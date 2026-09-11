# Instrucciones para agentes del proyecto

## Estructura del proyecto

- `Optica.Api/` contiene la API de .NET. Mantén los mapeos de base de datos en `Data/` y el código de dominio en `Modules/`.
- `Optica_front/` contiene la aplicación Angular.
- `database/schema.sql` contiene el script de inicialización de la base de datos.
- `docker-compose.yml` coordina la API y MySQL.

## Contraseñas y configuración local

- Nunca escribas contraseñas, tokens ni cadenas de conexión reales directamente en archivos versionados. No los muestres en respuestas, registros ni salidas de herramientas.
- Guarda las credenciales locales en `.env`, excluido de Git, o en variables de entorno. Conserva los valores locales existentes.
- Versiona `.env.example` únicamente con valores vacíos o marcadores de ejemplo, nunca con credenciales reales.
- Docker Compose debe obtener `MYSQL_ROOT_PASSWORD` del entorno o de `.env`, sin una contraseña predeterminada.
- Para ejecutar la API directamente, configura `ConnectionStrings__OpticaDb` mediante variables de entorno o secretos de usuario de .NET. No guardes credenciales en archivos `appsettings` versionados.
- Antes de preparar cambios para un commit, verifica que Git ignore los archivos de secretos y que los cambios no contengan credenciales. Realiza estas comprobaciones sin imprimir valores secretos.
- Si una credencial ya fue incluida en un commit, eliminarla del archivo actual no la elimina del historial. Informa al usuario y no reescribas el historial sin autorización.

## Implementación y validación

- No edites manualmente carpetas generadas como `bin/`, `obj/`, `node_modules/` o `dist/`.
- Conserva los nombres existentes en español de tablas y columnas, salvo que el cambio incluya una migración de base de datos.
- Usa inglés para identificadores del código, nombres de archivos, nuevas rutas de API y comentarios técnicos.
- Usa PascalCase para tipos y miembros públicos de C#; usa camelCase para variables locales y miembros de TypeScript.
- Conserva los textos de la interfaz en español y la compatibilidad con los clientes existentes al cambiar nombres.
- Revisa los cambios y ejecuta las comprobaciones de compilación pertinentes antes de crear un commit.
