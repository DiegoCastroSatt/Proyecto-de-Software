using Optica.Api.Modules.Appointments.Models;
public class MemoryAppointmentRepository : IAppointmentRepository
{
    private readonly List<Appointment> _appointments = [];
    private int _nextId;

    public Task<bool> ExistsAt(DateTime date, TimeSpan time)
    {
        bool exists = _appointments.Any(appointment =>
            appointment.Date.Date == date.Date &&
            appointment.Time == time &&
            appointment.Status != "Cancelada");

        return Task.FromResult(exists);
    }

    public Task<Appointment> Create(Appointment appointment)
    {
        appointment.Id = Interlocked.Increment(ref _nextId);
        _appointments.Add(appointment);
        return Task.FromResult(appointment);
    }

    public Task<Appointment?> GetById(int id)
    {
        return Task.FromResult(_appointments.FirstOrDefault(appointment => appointment.Id == id));
    }
}
