using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed record ResultadoInicioSesion(string TipoUsuario, LoginAdministradorResponse? Personal = null,
    ClienteSesionResponse? Cliente = null);

public sealed class InicioSesionService(OpticaDbContext db,
    IAutenticacionAdministradorService administradores, AccesoService trabajadores,
    AutenticacionClienteService clientes)
{
    public async Task<ResultadoInicioSesion?> IniciarAsync(LoginClienteRequest request, CancellationToken ct)
    {
        var identificador = request.Correo.Trim();
        var normalizado = identificador.ToUpperInvariant();
        var resultados = new List<ResultadoInicioSesion>();
        // Conservar el usuario administrativo existente y aceptar también su correo.
        var cuentasAdmin = await db.Administradores.AsNoTracking()
            .Where(a => a.Nombre.Trim().ToUpper() == normalizado || a.Correo.Trim().ToUpper() == normalizado)
            .Select(a => a.Nombre).ToListAsync(ct);
        foreach (var nombre in cuentasAdmin)
        {
            var sesion = await administradores.IniciarSesionAsync(new LoginAdministradorRequest
                { NombreUsuario = nombre, Contrasena = request.Contrasena }, ct);
            if (sesion is not null) resultados.Add(new("Administrador", Personal: sesion));
        }
        var trabajador = await trabajadores.IniciarTrabajadorAsync(new LoginAdministradorRequest
            { NombreUsuario = identificador, Contrasena = request.Contrasena }, ct);
        if (trabajador is not null) resultados.Add(new("Trabajador", Personal: trabajador));
        var (cliente, valida) = await clientes.IniciarSesionAsync(request, ct);
        if (valida && cliente is not null) resultados.Add(new("Cliente", Cliente: cliente));
        // No elegir una identidad por prioridad si las mismas credenciales corresponden a varias cuentas.
        return resultados.Count == 1 ? resultados[0] : null;
    }
}
