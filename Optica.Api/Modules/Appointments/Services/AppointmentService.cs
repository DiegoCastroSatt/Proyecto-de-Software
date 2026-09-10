using Optica.Api.Modules.Appointments.Models;
public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;

    public AppointmentService(
        IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    public async Task<AppointmentResponse> CreateAppointment(
        CreateAppointmentRequest request)
    {
        if (request.Date.Date < DateTime.Today)
        {
            throw new Exception(
                "No se puede reservar una fecha pasada."
            );
        }

        bool exists = await _appointmentRepository
            .ExistsAt(request.Date, request.Time);

        if (exists)
        {
            throw new Exception(
                "La hora seleccionada ya está reservada."
            );
        }

        var appointment = new Appointment
        {
            CustomerId = 1, // Temporary until customer creation is implemented.
            Date = request.Date,
            Time = request.Time,
            Status = "Pendiente"
        };

        var createdAppointment =
            await _appointmentRepository.Create(appointment);

        return new AppointmentResponse
        {
            Id = createdAppointment.Id,
            Date = createdAppointment.Date,
            Time = createdAppointment.Time,
            Status = createdAppointment.Status
        };
    }
}
