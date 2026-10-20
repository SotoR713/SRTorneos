using SRTorneos.Models;

namespace SRTorneos.Repositories;

public interface ICatalogoRepository
{
    Task<List<Deporte>> ObtenerDeportesAsync(CancellationToken cancellationToken = default);
    Task<List<TipoTorneo>> ObtenerTiposTorneoAsync(CancellationToken cancellationToken = default);
    Task<List<ElementoCatalogo>> ObtenerEstadosAsync(CancellationToken cancellationToken = default);
    Task<List<ElementoCatalogo>> ObtenerPuestosAsync(CancellationToken cancellationToken = default);
    Task<List<ElementoCatalogo>> ObtenerTiposFaltaAsync(CancellationToken cancellationToken = default);

    // Administración genérica de catálogos simples (/catalogos)
    Task<List<ElementoCatalogoUso>> ObtenerConUsoAsync(CatalogoDefinicion catalogo, CancellationToken cancellationToken = default);
    Task<int> CrearAsync(CatalogoDefinicion catalogo, string valor, CancellationToken cancellationToken = default);
    Task<bool> ActualizarAsync(CatalogoDefinicion catalogo, int id, string valor, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(CatalogoDefinicion catalogo, int id, CancellationToken cancellationToken = default);
}
