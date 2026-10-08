using SRTorneos.Models;

namespace SRTorneos.Repositories;

public interface IEquipoRepository
{
    // Catálogo general de equipos
    Task<List<EquipoResumen>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
    Task<Equipo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreAsync(string nombre, int excluirId = 0, CancellationToken cancellationToken = default);
    Task<int> CrearAsync(string nombre, CancellationToken cancellationToken = default);
    Task<bool> ActualizarAsync(Equipo equipo, CancellationToken cancellationToken = default);
    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default);

    // Inscripciones a un torneo
    Task<List<EquipoInscrito>> ObtenerInscritosAsync(int idTorneo, CancellationToken cancellationToken = default);
    Task<EquipoInscrito?> ObtenerInscritoAsync(int idTorneo, int idEquipo, CancellationToken cancellationToken = default);
    Task<List<Equipo>> ObtenerNoInscritosAsync(int idTorneo, CancellationToken cancellationToken = default);
    Task InscribirAsync(int idTorneo, int idEquipo, int idEstado, int idPuesto, CancellationToken cancellationToken = default);
    Task<int> CrearEInscribirAsync(int idTorneo, string nombre, int idEstado, int idPuesto, CancellationToken cancellationToken = default);
    Task<bool> ActualizarInscripcionAsync(int idInscripcion, int idTorneo, int idEstado, int idPuesto, CancellationToken cancellationToken = default);

    /// <summary>Quita al equipo del torneo junto con su alineación. Falla si ya tiene partidos en el torneo.</summary>
    Task<bool> QuitarDelTorneoAsync(int idTorneo, int idEquipo, CancellationToken cancellationToken = default);
}
