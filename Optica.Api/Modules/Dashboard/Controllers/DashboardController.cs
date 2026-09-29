using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;

namespace Optica.Api.Modules.Dashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController(OpticaDbContext context) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var customerCount = await context.Clientes.CountAsync(cancellationToken);
        var lowStockProductCount = await context.Productos
            .CountAsync(product => product.Stock <= product.StockMinimo, cancellationToken);
        var pendingOrderCount = await context.Pedidos
            .CountAsync(order => order.Estado == "Pendiente" || order.Estado == "En proceso", cancellationToken);
        var todaySales = await context.Ventas
            .Where(sale => sale.Fecha >= today && sale.Fecha < tomorrow)
            .Select(sale => (decimal?)sale.Total)
            .SumAsync(cancellationToken) ?? 0;

        return Ok(new DashboardSummaryResponse(
            customerCount,
            lowStockProductCount,
            pendingOrderCount,
            todaySales,
            DateTime.UtcNow));
    }
}

public sealed record DashboardSummaryResponse(
    int CustomerCount,
    int LowStockProductCount,
    int PendingOrderCount,
    decimal TodaySales,
    DateTime UpdatedAt);
