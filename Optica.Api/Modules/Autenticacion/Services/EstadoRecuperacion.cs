using System.Collections.Concurrent;

namespace Optica.Api.Modules.Autenticacion.Services;

/// <summary>Conserva códigos temporales solo durante la vida del proceso de la API.</summary>
public sealed class EstadoRecuperacion
{
    private readonly ConcurrentDictionary<string, Desafio> desafios = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> bloqueos = new(StringComparer.Ordinal);

    public SemaphoreSlim ObtenerBloqueo(string clave) => bloqueos.GetOrAdd(clave, _ => new SemaphoreSlim(1, 1));

    public Desafio? Obtener(string clave) => desafios.GetValueOrDefault(clave);

    public void Guardar(string clave, Desafio desafio) => desafios[clave] = desafio;

    public void Invalidar(string clave) => desafios.TryRemove(clave, out _);

    public void LimpiarExpirados(DateTime ahoraUtc)
    {
        foreach (var elemento in desafios)
        {
            if (elemento.Value.ExpiraUtc <= ahoraUtc)
                ((ICollection<KeyValuePair<string, Desafio>>)desafios).Remove(elemento);
        }
    }

    public sealed record Desafio(string Codigo, DateTime ExpiraUtc, int Intentos);
}
