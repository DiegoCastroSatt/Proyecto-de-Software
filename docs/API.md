# API HTTP

La API ASP.NET Core registra Swagger (`/swagger`). Compose no fija un `ASPNETCORE_ENVIRONMENT` para la API principal, por lo que el middleware Swagger queda activo allí; no hay autenticación Swagger configurada. Base URL local Docker: `http://localhost:8080`. Las rutas usan JSON salvo las señaladas como formulario/carga. Los cuerpos se modelan en los DTOs de `Optica.Api/Modules`; los atributos de validación de cada DTO y las verificaciones de servicio determinan los detalles. En general, controladores retornan `200`, `201`, `204`, `400`, `404` o `409` según la acción y el resultado; autenticación/autorización devuelve `401`/`403`. Véase código de acción para cada caso y no asumir un único formato de error global.

Autenticación significa cookie HTTP del esquema de personal o cliente; no bearer token. `p` denota parámetro de ruta y `q` de consulta. Las acciones heredan políticas de clase salvo `[AllowAnonymous]` o política distinta especificada.

## Autenticación y cuenta

| Método y ruta | Acceso | Parámetros/cuerpo | Respuesta observada |
|---|---|---|---|
| POST `/api/Autenticacion/login` | Público | `LoginClienteRequest` | Resultado de inicio de sesión; establece cookie según el tipo de cuenta. |
| POST `/api/Autenticacion/admin` | Público | Request de login administrativo | `LoginAdministradorResponse`; cookie personal. |
| POST `/api/Autenticacion/admin/recuperacion/solicitar` | Público | Request de recuperación | Resultado de solicitud. |
| POST `/api/Autenticacion/admin/recuperacion/confirmar` | Público | Request con código y nueva contraseña | Resultado de confirmación. |
| GET `/api/Autenticacion/admin/sesion` | Cookie personal | — | Sesión personal. |
| POST `/api/Autenticacion/admin/salir` | Cookie personal | — | Cierre de sesión. |
| POST `/api/Autenticacion/recuperacion/solicitar` | Público | `SolicitarRecuperacionRequest` | Resultado de solicitud para login unificado. |
| POST `/api/Autenticacion/recuperacion/confirmar` | Público | `ConfirmarRecuperacionRequest` | Resultado de confirmación. |
| POST `/api/Autenticacion/cliente/login` | Público | Request de login cliente | Inicio de sesión/cookie cliente. |
| POST `/api/Autenticacion/cliente/registro` | Público | DTO de registro cliente | Registro pendiente/resultado. |
| POST `/api/Autenticacion/cliente/registro/confirmar` | Público | DTO de confirmación | Confirma registro. |
| POST `/api/Autenticacion/cliente/registro/confirmacion/reenviar` | Público | DTO de reenvío | Reenvía confirmación. |
| POST `/api/Autenticacion/cliente/recuperacion/solicitar` | Público | DTO de solicitud | Solicita recuperación. |
| POST `/api/Autenticacion/cliente/recuperacion/confirmar` | Público | DTO de confirmación | Confirma recuperación. |
| GET `/api/Autenticacion/cliente/sesion` | Cookie cliente | — | Sesión cliente. |
| POST `/api/Autenticacion/cliente/correo/cambio` | Cookie cliente | DTO cambio de correo | Inicia/cambia correo conforme servicio. |
| DELETE `/api/Autenticacion/cliente/cuenta` | Cookie cliente | — | Elimina/desactiva la cuenta según servicio. |
| POST `/api/Autenticacion/cliente/salir` | Cookie cliente | — | Cierre de sesión. |
| POST `/api/CuentaTrabajador/contrasena` | Personal con tipo Trabajador | `CambiarContrasenaTrabajadorRequest` | Cambio de contraseña. |

## Reservas y horarios

| Método y ruta | Acceso | Parámetros/cuerpo | Respuesta/efecto |
|---|---|---|---|
| GET `/api/Reservas/disponibles?fecha=&excluirReservaId=` | Público | `fecha` opcional en formato `yyyy-MM-dd`; `excluirReservaId` opcional | Horarios disponibles; fecha inválida da `400`. |
| POST `/api/Reservas` | Público | DTO de creación de reserva | Crea reserva y encola notificación; estado inicial conforme servicio. |
| GET `/api/Reservas/agenda?historialAtendidas=` | Permiso `agenda` | `historialAtendidas` booleano opcional, por defecto `false` | `ReservaAgendaResponseDto[]`. |
| PUT `/api/Reservas/{id}/reprogramar` | `agenda` | `ReprogramarReservaDto` | Reprograma reserva. |
| POST `/api/Reservas/{id}/cancelar` | `agenda` | `id` | Cancela reserva administrativamente. |
| GET `/api/Reservas/acciones/confirmar/{token}` | Público, token de correo | `token` en ruta | Página HTML intermedia; no ejecuta el cambio por GET. |
| POST `/api/Reservas/acciones/confirmar/{token}` | Público, token | `token` en ruta | Ejecuta confirmación y devuelve página/resultado. |
| GET `/api/Reservas/acciones/cancelar/{token}` | Público, token | `token` en ruta | Página HTML intermedia. |
| POST `/api/Reservas/acciones/cancelar/{token}` | Público, token | `token` en ruta | Ejecuta cancelación. |
| GET `/api/Horarios?fecha=` | `horarios` | `fecha` opcional, `yyyy-MM-dd`; lista solo horarios futuros | Horarios administrativos. |
| POST `/api/Horarios` | `horarios` | `CrearHorariosDto` | Crea bloques futuros y devuelve la lista. |
| PATCH `/api/Horarios/{idHorario}/estado` | `horarios` | `idHorario`, `CambiarEstadoHorarioDto` | Actualiza estado. |

## Clientes, recetas, graduaciones y perfil

| Método y ruta | Acceso | Parámetros/cuerpo | Resultado |
|---|---|---|---|
| GET `/api/Clientes/buscar?termino=` | `clientes` | `termino` opcional | Clientes encontrados. |
| POST `/api/Clientes` | `clientes` | `CrearClienteDto` | Registra cliente. |
| PUT `/api/Clientes/{id}` | `clientes` | `ActualizarClienteDto` | Actualiza cliente. |
| PATCH `/api/Clientes/{id}/estado` | `clientes` | `CambiarEstadoDto` | Cambia estado. |
| GET `/api/Clientes/{id}/historial` | `clientes` | ID | `HistorialClienteDto`. |
| GET `/api/Clientes/perfil` | Cookie cliente | — | `PerfilClienteDto` propio. |
| PUT `/api/Clientes/perfil` | Cookie cliente | `ActualizarPerfilClienteDto` | `PerfilClienteDto` actualizado. |
| GET `/api/Recetas/clientes/sugerencias?termino=` | Personal con permiso clientes, ventas, recetas o pedidos | `termino` | Sugerencias de clientes. |
| GET `/api/Recetas/historial?rut=` | `recetas` | RUT | Recetas/historial. |
| GET `/api/Recetas/mis-recetas` | Cookie cliente | — | Recetas propias. |
| GET `/api/Recetas/{id}` | `recetas` | ID | Receta. |
| POST `/api/Recetas` | `recetas` | `multipart/form-data`, `CrearRecetaDto` | Crea receta/graduaciones. |
| PUT `/api/Recetas/{id}` | `recetas` | `multipart/form-data`, `CrearRecetaDto` | Actualiza receta. |

## Productos, marcos, rostros y catálogos

| Método y ruta | Acceso | Parámetros/cuerpo | Resultado |
|---|---|---|---|
| GET `/api/Productos?termino=` | Público | `termino` | Búsqueda de productos. |
| GET `/api/Productos/{id}` | Público | ID | Producto o `404`. |
| POST `/api/Productos` | `inventario` | Formulario `CrearProductoDto` | Crea producto/carga asociada. |
| POST `/api/Productos/json` | `inventario` | `CrearProductoDto` JSON | Crea producto. |
| PUT `/api/Productos/{id}` | `inventario` | Formulario `ActualizarProductoDto` | Actualiza producto. |
| PATCH `/api/Productos/{id}/stock` | `inventario` | `ActualizarStockDto` | Actualiza stock y registra historial. |
| GET `/api/Productos/{id}/historial-stock?pagina=&tamanoPagina=` | `inventario` | ID, `pagina` (1) y `tamanoPagina` (20) | Historial paginado del producto. |
| GET `/api/Productos/historial-stock?pagina=&tamanoPagina=` | `inventario` | `pagina` (1), `tamanoPagina` (20) | Historial reciente paginado. |
| DELETE `/api/Productos/{id}` | `inventario` | ID | Elimina producto si las reglas lo permiten. |
| GET `/api/Marcos` | `inventario` | — | Marcos. |
| GET `/api/Marcos/disponibles` | Público | — | Marcos disponibles. |
| GET `/api/Marcos/formas-lente` | `inventario` | — | Formas de lente. |
| GET `/api/Marcos/{id}/imagen` | Público | ID | Archivo de imagen. |
| POST `/api/Marcos/validar-diseno` | `inventario` | `ValidarDisenoMarcoRequest` | Valida diseño. |
| POST `/api/Marcos` | `inventario` | `CrearTipoMarcoRequest` | Crea marco. |
| PATCH `/api/Marcos/{id}/estado` | `inventario` | `EstadoTipoMarcoRequest` | Cambia estado. |
| GET `/api/Marcos/productos-vinculables` | `inventario` | — | Productos que se pueden vincular. |
| PUT `/api/Marcos/{id}/productos` | `inventario` | `VincularProductosMarcoRequest` | Asocia productos. |
| DELETE `/api/Marcos/{id}` | `inventario` | ID | Elimina marco. |
| GET `/api/Rostros` | `inventario` | — | Tipos de rostro administrativos. |
| GET `/api/Rostros/activos` | Público | — | Tipos activos. |
| GET `/api/Rostros/{id}/imagen` | Público | ID | Imagen. |
| POST `/api/Rostros` | `inventario` | Formulario `CrearTipoRostroRequest` | Crea tipo e imagen. |
| PATCH `/api/Rostros/{id}/estado` | `inventario` | `EstadoTipoMarcoRequest` | Cambia estado. |
| PUT `/api/Rostros/{id}/marcos` | `inventario` | `AsociarMarcosRequest` | Asocia marcos. |
| DELETE `/api/Rostros/{id}` | `inventario` | ID | Elimina tipo. |
| GET `/api/Catalogos?tipo=` | `inventario` | `tipo` requerido | Opciones del catálogo. |
| POST `/api/Catalogos` | `inventario` | `CrearCatalogoItemDto` | Crea opción. |

## Pedidos, ventas, reseñas y resumen

| Método y ruta | Acceso | Parámetros/cuerpo | Resultado |
|---|---|---|---|
| GET `/api/Pedidos` | `pedidos` | — | Pedidos. |
| GET `/api/Pedidos/clientes/buscar?rut=` | `pedidos` | `rut` requerido | Clientes encontrados para pedido. |
| GET `/api/Pedidos/clientes` | `pedidos` | — | Clientes para pedido. |
| GET `/api/Pedidos/cliente/{rut}` | `pedidos` | RUT | Pedidos/datos del cliente. |
| GET `/api/Pedidos/{id}` | `pedidos` | ID | Detalle. |
| POST `/api/Pedidos` | `pedidos` | DTO de creación | Crea pedido y encola notificación según servicio. |
| PATCH `/api/Pedidos/{id}/estado` | `pedidos` | ID y DTO de estado | Actualiza estado. |
| GET `/api/Ventas` | `ventas` | — | Ventas. |
| GET `/api/Ventas/producto?codigo=` | `ventas` | Código requerido | Busca producto para venta. |
| POST `/api/Ventas` | `ventas` | `CrearVentaDto` | Registra venta. |
| GET `/api/Resenas/producto/{idProducto}` | Público | ID producto | Reseñas. |
| GET `/api/Resenas/mis-productos` | Cookie cliente | — | Productos que el cliente puede reseñar. |
| POST `/api/Resenas/producto/{idProducto}` | Cookie cliente | `CrearResenaDto` | Crea reseña; unicidad y calificación se validan. |
| GET `/api/Dashboard/summary` | `resumen` | — | `DashboardSummaryResponse`. |

La ruta Angular de consulta de pedidos cliente usa `GET /api/Pedidos/cliente/{rut}`, pero este endpoint hereda `pedidos` del controlador. Por ello, la interfaz cliente no puede consumirlo con la cookie de cliente: esta diferencia está registrada como funcionalidad parcial y no se presenta como acceso cliente implementado.

## Administración de accesos

Todas requieren política `SoloAdministrador`.

| Método y ruta | Parámetros/cuerpo | Resultado |
|---|---|---|
| GET `/api/Accesos/permisos` | — | Catálogo de permisos. |
| GET `/api/Accesos/roles` | — | Roles configurados. |
| POST `/api/Accesos/roles` | `RolRequest` | Crea rol. |
| PUT `/api/Accesos/roles/{id}` | ID, `RolRequest` | Actualiza rol. |
| GET `/api/Accesos/trabajadores` | — | Lista de personal, excluye administrador. |
| POST `/api/Accesos/trabajadores` | `CrearTrabajadorRequest` | Crea trabajador. |
| PUT `/api/Accesos/trabajadores/{id}/acceso` | ID, `AsignarAccesoRequest` | Asigna acceso/rol. |

## Endpoints de infraestructura/pruebas

`GET /test-db` devuelve conteo de clientes tras consultar MySQL; no es un endpoint de negocio. `GET /health/db` existe únicamente con `ASPNETCORE_ENVIRONMENT=E2E` y verifica conectividad. No se debe usar la API de E2E contra una base desplegada.

La tabla resume rutas y contratos de nivel controlador; para propiedades obligatorias, reglas y códigos específicos, revisar DTO, validaciones y rama de cada acción. No se ha detectado un filtro global común de errores que permita prometer un formato único para todas las respuestas.
