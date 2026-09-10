using Optica.Api.Modules.Appointments.Models;
public interface IAppointmentRepository
{
    Task<bool> ExistsAt(
        DateTime date,
        TimeSpan time
    );

    Task<Appointment> Create(Appointment appointment);

    Task<Appointment?> GetById(int id);
}
