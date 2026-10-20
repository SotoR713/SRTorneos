using SRTorneos.Models;

namespace SRTorneos.Repositories;

public interface IPartidoRepository
{
    Task<List<PartidoResumen>> ObtenerPorTorneoAsync(int idTorneo, CancellationToken cancellationToken = default);
    /// <summary>Partidos de todos los torneos en un rango de fechas (agenda pública, canchas, portada).</summary>
    Task<List<PartidoResumen>> ObtenerAgendaAsync(FiltroAgenda filtro, CancellationToken cancellationToken = default);

    Task<PartidoResumen?> ObtenerAsync(int idTorneo, int idPartido, CancellationToken cancellationToken = default);
    Task<int> CrearAsync(Partido partido, CancellationToken cancellationToken = default);
    Task<bool> ActualizarAsync(Partido partido, CancellationToken cancellationToken = default);

    /// <summary>Elimina el partido con su marcador, anotaciones y faltas.</summary>
    Task<bool> EliminarAsync(int idTorneo, int idPartido, CancellationToken cancellationToken = default);

    /// <summary>Guarda el marcador y una fila en Anotacion por cada anotación (canasta, gol...).</summary>
    Task GuardarResultadoAsync(PartidoResumen partido, int puntos1, int puntos2,
        IReadOnlyCollection<CanastasJugador> anotaciones, CancellationToken cancellationToken = default);
    Task BorrarResultadoAsync(int idPartido, CancellationToken cancellationToken = default);
    Task<List<CanastasJugador>> ObtenerAnotacionesAsync(int idPartido, CancellationToken cancellationToken = default);

    Task<List<FaltaRegistrada>> ObtenerFaltasAsync(int idPartido, CancellationToken cancellationToken = default);
    Task RegistrarFaltaAsync(int idTorneo, int idPartido, int idJugador, int idTipoFalta, CancellationToken cancellationToken = default);
    Task<bool> EliminarFaltaAsync(int idPartido, int idFalta, CancellationToken cancellationToken = default);

    /// <summary>Guarda un calendario completo. Si se indica, antes borra los partidos sin resultado.</summary>
    Task<int> CrearVariosAsync(int idTorneo, IReadOnlyList<Partido> partidos, bool reemplazarSinResultado, CancellationToken cancellationToken = default);

    /// <summary>Tabla de posiciones del torneo.</summary>
    Task<List<FilaPosicion>> ObtenerPosicionesAsync(int idTorneo, int puntosVictoria, CancellationToken cancellationToken = default);
}

/// <param name="Jugados">true = solo con resultado; false = solo pendientes; null = todos.</param>
/// <param name="Torneos">Si se indica, solo esos torneos (lista vacía = ninguno).</param>
public sealed record FiltroAgenda(
    DateTime Desde,
    DateTime Hasta,
    bool? Jugados = null,
    int Maximo = 100,
    bool Descendente = false,
    int? IdLugar = null,
    IReadOnlyCollection<int>? Torneos = null);
