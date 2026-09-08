using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Data;

namespace Optica.Api.Tdd.Tests.Soporte;

public sealed class OpticaApiFactory : WebApplicationFactory<Program>
{
    private readonly string _nombreBaseDatos = $"optica-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var configuracionDb = services.SingleOrDefault(descriptor =>
                descriptor.ServiceType == typeof(DbContextOptions<OpticaDbContext>));

            if (configuracionDb is not null)
            {
                services.Remove(configuracionDb);
            }

            services.AddDbContext<OpticaDbContext>(opciones =>
                opciones.UseInMemoryDatabase(_nombreBaseDatos));
        });
    }
}
