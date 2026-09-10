using Optica.Api.Data;
using Optica.Api.Modules.Appointments.Models;
using Microsoft.EntityFrameworkCore;
public class AppointmentRepository : IAppointmentRepository
{
    private readonly OpticaDbContext _context;

    public AppointmentRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAt(
        DateTime date,
        TimeSpan time)
    {
        return await _context.Appointments
            .AnyAsync(appointment =>
                appointment.Date.Date == date.Date &&
                appointment.Time == time &&
                appointment.Status != "Cancelada"
            );
    }

    public async Task<Appointment> Create(Appointment appointment)
    {
        _context.Appointments.Add(appointment);

        await _context.SaveChangesAsync();

        return appointment;
    }

    public async Task<Appointment?> GetById(int id)
    {
        return await _context.Appointments
            .FirstOrDefaultAsync(appointment => appointment.Id == id);
    }
}
