using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Optica.Api.Data;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.Services;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed class AutenticacionClienteService(
    OpticaDbContext db,
    IPasswordHasher<CuentaCliente> passwordHasher)
{
    public async Task<(ClienteSesionResponse? Sesion, bool CredencialesValidas)> IniciarSesionAsync(
        LoginClienteRequest request, CancellationToken cancellationToken)
    {
        var correo = NormalizarCorreo(request.Correo);
        var cuenta = await db.CuentasCliente
            .Include(c => c.Cliente)
            .SingleOrDefaultAsync(c => c.CorreoNormalizado == correo, cancellationToken);

        if (cuenta is null || cuenta.Estado != "Activa" || cuenta.Cliente?.Estado != "Activo")
            return (null, false);

        var resultado = passwordHasher.VerifyHashedPassword(cuenta, cuenta.ContrasenaHash, request.Contrasena);
        if (resultado == PasswordVerificationResult.Failed)
            return (null, false);

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            cuenta.ContrasenaHash = passwordHasher.HashPassword(cuenta, request.Contrasena);
            await db.SaveChangesAsync(cancellationToken);
        }

        return (CrearSesion(cuenta), true);
    }

    public async Task<ResultadoRegistroCliente> RegistrarAsync(
        RegistroClienteRequest request, CancellationToken cancellationToken)
    {
        var nombre = request.Nombre.Trim();
        var apellido = request.Apellido.Trim();
        var rut = RutChilenoValidator.Normalizar(request.Rut);
        var correo = request.Correo.Trim();
        var correoNormalizado = NormalizarCorreo(correo);
        var telefono = request.Telefono.Trim();

        if (nombre.Length == 0 || apellido.Length == 0)
            return ResultadoRegistroCliente.Invalido("Ingresa nombre y apellido.");
        if (!RutChilenoValidator.EsValido(request.Rut))
            return ResultadoRegistroCliente.Invalido("El RUT ingresado no es válido.");
        if (correo.Length > 100 || !MailAddress.TryCreate(correo, out var direccion)
            || !string.Equals(direccion.Address, correo, StringComparison.OrdinalIgnoreCase))
            return ResultadoRegistroCliente.Invalido("Ingresa un correo electrónico válido de hasta 100 caracteres.");
        if (!TelefonoChilenoValidator.EsValido(telefono))
            return ResultadoRegistroCliente.Invalido("Ingresa un celular válido de 9 dígitos; puedes incluir el prefijo +56.");
        if (!ContrasenaValida(request.Contrasena))
            return ResultadoRegistroCliente.Invalido("La contraseña debe tener al menos 8 caracteres e incluir letras, números y un símbolo.");
        if (!string.Equals(request.Contrasena, request.ConfirmarContrasena, StringComparison.Ordinal))
            return ResultadoRegistroCliente.Invalido("Las contraseñas no coinciden.");

        var rutExistente = await db.Clientes.AnyAsync(c =>
            c.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper() == rut, cancellationToken);
        var correoExistente = await db.Clientes.AnyAsync(c => c.Correo != null
            && c.Correo.Trim().ToUpper() == correoNormalizado, cancellationToken);
        var cuentaExistente = await db.CuentasCliente.AnyAsync(c =>
            c.CorreoNormalizado == correoNormalizado, cancellationToken);
        if (rutExistente || correoExistente || cuentaExistente)
            return ResultadoRegistroCliente.Conflicto();

        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var cliente = new Cliente
            {
                Rut = rut,
                Nombre = nombre,
                Apellido = apellido,
                Correo = correo,
                Telefono = telefono,
                Estado = "Activo",
                FechaRegistro = DateTime.Now
            };
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync(cancellationToken);

            var cuenta = new CuentaCliente
            {
                IdCliente = cliente.IdCliente,
                Correo = correo,
                CorreoNormalizado = correoNormalizado,
                Estado = "Activa",
                FechaCreacion = DateTime.UtcNow
            };
            cuenta.ContrasenaHash = passwordHasher.HashPassword(cuenta, request.Contrasena);
            db.CuentasCliente.Add(cuenta);
            await db.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return ResultadoRegistroCliente.Exitoso();
        }
        catch (DbUpdateException ex) when (EsDuplicadoMySql(ex))
        {
            await transaccion.RollbackAsync(cancellationToken);
            return ResultadoRegistroCliente.Conflicto();
        }
    }

    public static ClienteSesionResponse CrearSesion(CuentaCliente cuenta) => new(
        cuenta.IdCliente,
        $"{cuenta.Cliente?.Nombre} {cuenta.Cliente?.Apellido}".Trim(),
        cuenta.Correo);

    public static string NormalizarCorreo(string correo) => correo.Trim().ToUpperInvariant();

    private static bool ContrasenaValida(string contrasena) =>
        contrasena.Length is >= 8 and <= 255
        && contrasena.Any(char.IsLetter)
        && contrasena.Any(char.IsDigit)
        && contrasena.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

    private static bool EsDuplicadoMySql(Exception exception)
    {
        for (Exception? actual = exception; actual is not null; actual = actual.InnerException)
            if (actual is MySqlException mysql && mysql.Number == 1062)
                return true;
        return false;
    }
}

public sealed record ResultadoRegistroCliente(bool Registrado, string? Error, bool ConflictoExistente)
{
    public static ResultadoRegistroCliente Exitoso() => new(true, null, false);
    public static ResultadoRegistroCliente Invalido(string error) => new(false, error, false);
    public static ResultadoRegistroCliente Conflicto() => new(false, null, true);
}
