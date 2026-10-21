# Trabajadores, roles y permisos

El administrador conserva acceso total. En su nombre, en la barra superior, se abre un menú con **Trabajadores** y **Cerrar sesión**. Un trabajador tiene las opciones **Cambiar contraseña** y **Cerrar sesión** y no puede administrar accesos.

## Uso

1. Entrar con una cuenta administradora existente.
2. Abrir el menú del nombre y seleccionar **Trabajadores**.
3. En **Roles y permisos**, crear un rol y marcar sus funciones. Cada permiso habilita las operaciones del módulo indicado; el resumen general incluye indicadores de todos los módulos.
4. En **Trabajadores**, pulsar **Agregar trabajador**, completar nombre, correo, contraseña inicial de al menos 12 caracteres y rol.
5. Comunicar las credenciales por un medio privado. El sistema no envía correos de invitación. El trabajador inicia sesión con su correo en el formulario existente.
6. Cambiar rol o estado y pulsar **Guardar acceso**. Editar un rol afecta a todos los trabajadores que lo tienen asignado.

Las cuentas están separadas de clientes y administradores. No se incorpora registro ni autenticación de clientes en este cambio. La recuperación de contraseña existente sigue correspondiendo a administradores.

## Base de datos existente y Docker

La migración es aditiva y no elimina datos. No se aplica automáticamente al arrancar la API. Desde la raíz del proyecto, con MySQL en ejecución:

```sh
docker compose exec -T database sh -c 'MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysql -uroot' < database/migrations/20261021_roles_trabajadores.sql
docker compose up -d --build backend frontend
```

El primer comando utiliza la credencial configurada dentro del contenedor, sin escribir su valor en el comando. En una instalación nueva, las tablas ya forman parte de `database/schema.sql`.

No usar `docker compose down -v`: no se necesita eliminar los volúmenes para esta actualización.

## Comportamiento del acceso

- El servidor relee rol, permisos y estado en cada petición autenticada. Las cuentas inactivas pierden su sesión en la siguiente petición.
- Las peticiones sin sesión reciben 401; las que no tienen permiso reciben 403.
- Las rutas internas y los accesos de la interfaz respetan los mismos módulos. Los cambios del rol se reflejan al navegar o ante un rechazo del servidor.
- La creación de roles, asignación de accesos y creación de trabajadores están reservadas exclusivamente al administrador.
- El catálogo público y las reservas públicas continúan disponibles.
- Las contraseñas de trabajadores se guardan con hash de ASP.NET Identity; no se devuelven en los listados.
- Los horarios creados por un trabajador se vinculan al administrador que creó su cuenta, conservando la relación existente de horarios con administradores.

## Validación

```sh
dotnet test tests/Optica_test/Optica.Tests.csproj
cd Optica_front
npm run build
npm test -- --watch=false
```

## Cambio de contraseña del trabajador

En el desplegable de su nombre, cada trabajador puede abrir **Cambiar contraseña**, indicar su contraseña actual y confirmar una nueva de al menos 12 caracteres. No necesita un permiso de módulo para acceder a su propia cuenta. La API obtiene la identidad de la sesión, sin aceptar un identificador ajeno. Tras guardar se cierra la sesión; las demás sesiones de ese trabajador también se invalidan en la siguiente petición. No se requiere una migración adicional para esta función.
