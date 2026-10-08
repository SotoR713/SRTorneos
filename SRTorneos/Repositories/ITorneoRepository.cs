using SRTorneos.Models;

namespace SRTorneos.Repositories;

public interface ITorneoRepository
{
    Task<List<Torneo>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
    Task<int> CrearAsync(Torneo torneo, CancellationToken cancellationToken = default);
    Task<Torneo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ActualizarAsync(Torneo torneo, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default);
}
