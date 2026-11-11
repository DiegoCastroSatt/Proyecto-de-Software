# Seguridad, autenticación y permisos

## Sesiones y protección

El backend utiliza autenticación por cookies HTTP, no JWT/Bearer. Personal usa `Optica.Personal`; clientes usan `Optica.Cliente`. Ambas cookies son `HttpOnly`, expiran a las ocho horas y no usan sliding expiration. En producción se exige HTTPS y `SameSite=None`; fuera de producción se usa `SameSite=Strict` y `Secure` sigue el esquema de la solicitud. Las claves de ASP.NET Data Protection pueden persistirse en `DataProtection:KeysPath`; Compose principal las guarda en el volumen `optica_data_protection_keys`.

El personal inicia sesión por `/admin` (también existe endpoint administrativo específico) y el cliente por `/cliente/login`; el login unificado `/api/Autenticacion/login` acepta credenciales según tipo. El backend vuelve a consultar acceso del personal en cada petición para que revocaciones surtan efecto; para clientes verifica que cuenta/cliente sigan activos y que la credencial no haya cambiado. Solicitudes de escritura con cookie requieren un origen frontend permitido (defensa CSRF del middleware). El backend responde 401 si no hay sesión y 403 si no hay autorización.

Los guards Angular (`auth-admin-guard`, `auth-cliente-guard`) controlan navegación y consultan sesión/permisos; son una protección de interfaz. Las políticas ASP.NET en controladores/acciones son la barrera efectiva de API. Existe política fallback administrativa para endpoints sin política explícita; excepciones públicas se marcan expresamente con `[AllowAnonymous]`.

## Tipos de usuario y permisos

Tipos de personal: `Administrador` y `Trabajador`. El administrador satisface todas las políticas de módulos. Un trabajador requiere el permiso asignado en su rol. El catálogo de permisos de código es `resumen`, `horarios`, `agenda`, `clientes`, `inventario`, `ventas`, `recetas`, `pedidos`. Además hay permisos internos `SoloAdministrador`, `CuentaTrabajador` y `ConsultarSugerenciasClientes` (este último permite clientes/ventas/recetas/pedidos). Cliente es un esquema separado sin permisos administrativos.

| Función | Administrador | Trabajador con permiso | Cliente autenticado | Público |
|---|---:|---:|---:|---:|
| Resumen | Sí | `resumen` | No | No |
| Horarios / agenda | Sí | `horarios` / `agenda` | No | Consultar horarios libres y crear reserva |
| Clientes / historial | Sí | `clientes` | Perfil propio | — |
| Productos | Sí | `inventario` para gestión | No requiere sesión para consulta | Consulta de productos |
| Recetas / graduaciones | Sí | `recetas` | Consulta propias | — |
| Pedidos | Sí | `pedidos` | Sin endpoint de pedidos con política cliente | No hay ruta pública de pedidos |
| Ventas | Sí | `ventas` | No | — |
| Catálogos, marcos y rostros | Sí | `inventario` | Recomendador/consulta pública disponible | Datos activos e imágenes públicas según endpoints |
| Reseñas | No aplica por permiso de módulo; administración no expuesta | — | Ver/crear según endpoints, propias | Leer reseñas por producto |
| Gestión de roles y trabajadores | Sí, exclusivo | No | No | No |
| Cambio de contraseña de trabajador | No por endpoint de trabajador | Sí, política tipo Trabajador | No | No |

La tabla expresa políticas de API y las rutas existentes; los componentes del frontend pueden limitar pantallas según los mismos permisos, pero no sustituyen los controles backend. La creación pública de reserva no exige sesión. No se publican credenciales, hashes, cookies ni valores secretos.

## Inconsistencia observada

Angular muestra `/consultar-pedido` dentro del área de cliente y envía la consulta a `GET /api/Pedidos/cliente/{rut}`. Sin embargo, `PedidosController` aplica en toda la clase la política `pedidos`, que solo autoriza personal administrativo con ese permiso. El guard de cliente permite abrir la pantalla, pero la API rechazará la llamada de cliente. Se documenta como flujo parcialmente implementado; no se modificó el código funcional en esta tarea.
