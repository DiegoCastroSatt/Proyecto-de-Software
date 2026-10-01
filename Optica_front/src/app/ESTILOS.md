# Estilos de la página

Los colores y estilos que se repiten están en `app.css`.

Para cambiar los colores, edita las variables al inicio del archivo, dentro de `:root`:

- `--color-principal`: color principal de la página.
- `--color-acento`: botones y detalles destacados.
- `--color-acento-hover`: color al pasar el mouse.
- `--color-fondo`: fondo de la página.
- `--color-superficie`: fondo de tarjetas y paneles.
- `--color-texto`: texto principal.
- `--color-texto-secundario`: texto de apoyo.
- `--color-error`: mensajes de error.

El cambio se aplica en todos los lugares que usan esa variable.

Cada componente conserva en su propio CSS los estilos que solo necesita esa vista. Antes de agregar uno nuevo, se tiene que revisar si ya existe en `app.css`.

Los colores de los PDF se configuran por separado.