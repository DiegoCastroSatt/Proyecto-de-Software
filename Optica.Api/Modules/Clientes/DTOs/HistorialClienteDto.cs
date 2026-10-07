namespace Optica.Api.Modules.Clientes.DTOs;

public class HistorialClienteDto
{
    public int IdCliente { get; set; }
    public string Rut { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string Estado { get; set; } = string.Empty;

    public List<HistorialRecetaDto> Recetas { get; set; } = [];
    public List<HistorialPedidoDto> Pedidos { get; set; } = [];
}

public class HistorialRecetaDto
{
    public int IdReceta { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observaciones { get; set; }
    public string? ImagenPath { get; set; }
    public List<HistorialGraduacionDto> Graduaciones { get; set; } = [];
}

public class HistorialGraduacionDto
{
    public string Ojo { get; set; } = string.Empty;
    public decimal? Esfera { get; set; }
    public decimal? Cilindro { get; set; }
    public int? Eje { get; set; }
    public decimal? Adicion { get; set; }
}

public class HistorialPedidoDto
{
    public int IdPedido { get; set; }
    public int? IdReceta { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? Anotaciones { get; set; }
}