using SRTorneos.Models;

namespace SRTorneos.Repositories;

public interface ILugarRepository
{
    Task<List<LugarResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
    Task<Lugar?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CrearAsync(Lugar lugar, CancellationToken cancellationToken = default);
    Task<bool> ActualizarAsync(Lugar lugar, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default);
}
