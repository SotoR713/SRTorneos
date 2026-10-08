using SRTorneos.Models;

namespace SRTorneos.Repositories;

public interface IJugadorRepository
{
    // Catálogo general de jugadores
    Task<List<JugadorResumen>> ObtenerTodosAsync(string? busqueda, CancellationToken cancellationToken = default);
    Task<int> CrearAsync(Jugador jugador, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default);

    // Plantillas por torneo
    Task<List<JugadorPlantilla>> ObtenerPlantillaAsync(int idTorneo, int idEquipo, CancellationToken cancellationToken = default);
    Task<List<Jugador>> ObtenerDisponiblesAsync(int idTorneo, CancellationToken cancellationToken = default);
    Task<Jugador?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Anotador>> ObtenerLideresAsync(int idTorneo, int cantidad, CancellationToken cancellationToken = default);


    Task<int> CrearYAlinearAsync(int idTorneo, int idEquipo, Jugador jugador, CancellationToken cancellationToken = default);
    Task AlinearAsync(int idTorneo, int idEquipo, int idJugador, CancellationToken cancellationToken = default);
    Task<bool> ActualizarAsync(Jugador jugador, CancellationToken cancellationToken = default);

    /// <summary>Quita al jugador de la alineación. Falla si ya tiene puntos o faltas en el torneo.</summary>
    Task<bool> QuitarDeAlineacionAsync(int idTorneo, int idEquipo, int idJugador, CancellationToken cancellationToken = default);
}
