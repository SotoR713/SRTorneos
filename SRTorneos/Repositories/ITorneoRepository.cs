using SRTorneos.Models;

namespace SRTorneos.Repositories;

public interface ITorneoRepository
{
    Task<List<TorneoResumen>> ObtenerResumenesAsync(CancellationToken cancellationToken = default);
    Task<TorneoResumen?> ObtenerResumenAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CrearAsync(Torneo torneo, int puntosVictoria, int equiposMax, CancellationToken cancellationToken = default);
    Task<bool> ActualizarAsync(Torneo torneo, int puntosVictoria, int equiposMax, CancellationToken cancellationToken = default);

    /// <summary>Elimina el torneo con sus partidos, resultados, faltas, alineaciones, inscripciones y configuración.</summary>
    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default);
}
