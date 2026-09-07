public class ReservaRepository : IReservaRepository
{
    private readonly OpticaDbContext _context;

    public ReservaRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExisteReserva(
        DateTime fecha,
        TimeSpan hora)
    {
        return await _context.Reservas
            .AnyAsync(r =>
                r.Fecha.Date == fecha.Date &&
                r.Hora == hora &&
                r.Estado != "Cancelada"
            );
    }

    public async Task<Reserva> Crear(Reserva reserva)
    {
        _context.Reservas.Add(reserva);

        await _context.SaveChangesAsync();

        return reserva;
    }

    public async Task<Reserva?> ObtenerPorId(int id)
    {
        return await _context.Reservas
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}