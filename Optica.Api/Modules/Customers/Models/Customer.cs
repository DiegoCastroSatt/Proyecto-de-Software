namespace Optica.Api.Modules.Customers.Models;

public class Customer
{
    public int Id { get; set; }

    public string NationalId { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string Status { get; set; } = "Activo";

    public DateTime RegisteredAt { get; set; }
}
