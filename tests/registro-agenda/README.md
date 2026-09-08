# Pruebas TDD: registro de agenda

Esta suite de pruebas unitarias permite aplicar testing temprano (shift-left) y TDD a la funcionalidad de agenda.

Casos cubiertos:

- Registrar una hora disponible y guardarla en la agenda.
- Rechazar una reserva en una hora ocupada e informar el motivo.
- Distinguir una hora ocupada de una disponible.
- Rechazar una reserva para una fecha pasada.

La funcionalidad que muestra una lista de horas disponibles aún no existe. La tercera prueba valida la lógica de disponibilidad que esa futura pantalla o endpoint deberá usar.

## Ejecutar

```bash
dotnet test tests/registro-agenda/Optica.Api.RegistroAgenda.Tests.csproj
```
