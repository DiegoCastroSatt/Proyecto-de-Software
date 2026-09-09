public interface IReservaService
{
    Task<ReservaResponseDto> CrearReserva(
        CrearReservaDto dto
    );
}