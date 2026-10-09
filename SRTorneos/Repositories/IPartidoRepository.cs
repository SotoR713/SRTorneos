using SRTorneos.Models;

namespace SRTorneos.Repositories;

public interface IPartidoRepository
{
    Task<List<PartidoResumen>> ObtenerPorTorneoAsync(int idTorneo, CancellationToken cancellationToken = default);
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

    Task<List<FilaPosicion>> ObtenerPosicionesAsync(int idTorneo, int puntosVictoria, CancellationToken cancellationToken = default);
}
