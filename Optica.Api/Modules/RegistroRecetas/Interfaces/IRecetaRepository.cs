using Optica.Api.Modules.RegistroRecetas.Models;

public interface IRecetaRepository
{
    Task<bool> ExisteCliente(int clienteId);
    Task<Receta> Crear(Receta receta);
    Task<Receta?> ObtenerPorId(int id);
}