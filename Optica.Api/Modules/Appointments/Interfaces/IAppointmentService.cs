public interface IAppointmentService
{
    Task<AppointmentResponse> CreateAppointment(
        CreateAppointmentRequest request
    );
}
