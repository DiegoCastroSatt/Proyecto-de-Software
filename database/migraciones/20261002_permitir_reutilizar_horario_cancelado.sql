-- Ejecutar una vez en bases de datos creadas antes de permitir reutilizar horarios cancelados.
-- La reserva cancelada permanece como registro; solo se elimina la restricción única del horario.
ALTER TABLE reservas
    DROP INDEX uq_reserva_horario;
