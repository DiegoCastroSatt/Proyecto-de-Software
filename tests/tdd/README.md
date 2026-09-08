# Pruebas TDD de la óptica

Esta suite usa pruebas de integración de la API con una base de datos temporal en memoria. Está organizada por los cinco módulos funcionales:

- `GestionClientes`: crear y buscar clientes.
- `RecetasYGraduaciones`: receta escrita, receta con imagen y graduación de ambos ojos.
- `Pedidos`: crear pedido con estado inicial y cambiar su estado.
- `Inventario`: registrar productos y consultar existencias.
- `AgendaYReservas`: crear una reserva y bloquear horas ocupadas.

Los módulos que todavía no tienen endpoints (`Clientes`, `Recetas`, `Graduaciones`, `Pedidos` e `Inventario`) están deliberadamente en rojo: las pruebas definen el contrato HTTP que debe implementar cada funcionalidad.

## Ejecutar

```bash
dotnet test tests/tdd/Optica.Api.Tdd.Tests.csproj
```
