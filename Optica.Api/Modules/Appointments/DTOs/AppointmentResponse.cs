public class AppointmentResponse
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public TimeSpan Time { get; set; }

    public string Status { get; set; } = string.Empty;
}
