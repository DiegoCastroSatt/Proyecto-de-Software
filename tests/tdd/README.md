# Pruebas TDD de la óptica

Las pruebas con una base de datos temporal en memoria. Organizada por las funcionales:

- `GestionClientes`: crear y buscar clientes.
- `RecetasYGraduaciones`: receta escrita, receta con imagen y graduación de ambos ojos.
- `Pedidos`: crear pedido con estado inicial y cambiar su estado.
- `Inventario`: registrar productos y consultar existencias.
- `AgendaYReservas`: crear una reserva y bloquear horas ocupadas.

## Ejecutar

```bash
dotnet test tests/tdd/Optica.Api.Tdd.Tests.csproj
```
