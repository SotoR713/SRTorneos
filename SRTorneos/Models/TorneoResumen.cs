namespace SRTorneos.Models;

public enum EstadoTorneo
{
    Proximo,
    EnCurso,
    Finalizado
}

/// <summary>Torneo con nombres de catálogos, configuración y conteos para mostrar en pantalla.</summary>
public class TorneoResumen
{
    public const int PuntosVictoriaPorDefecto = 2;

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int IdDeporte { get; set; }
    public string Deporte { get; set; } = string.Empty;
    public int IdTipoTorneo { get; set; }
    public string TipoTorneo { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public string Creador { get; set; } = string.Empty;
    public int? PuntosVict { get; set; }
    public int? EquiposMax { get; set; }
    public int Equipos { get; set; }
    public int Jugadores { get; set; }
    public int Partidos { get; set; }
    public int PartidosJugados { get; set; }
    public DateTime? PrimerPartido { get; set; }
    public DateTime? UltimoPartido { get; set; }

    public int PuntosVictoria => PuntosVict ?? PuntosVictoriaPorDefecto;

    /// <summary>El nombre del tipo sugiere eliminatoria; con la base actual se juega como liga (todos contra todos).</summary>
    public bool TipoPareceEliminatoria
        => new[] { "elimin", "copa", "llave", "playoff", "knock" }.Any(p => TipoTorneo.Contains(p, StringComparison.OrdinalIgnoreCase));
    public bool CupoLleno => EquiposMax is > 0 && Equipos >= EquiposMax;
    public int DuracionDias => FechaFin.DayNumber - FechaInicio.DayNumber + 1;

    public EstadoTorneo Estado
    {
        get
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            if (hoy < FechaInicio) return EstadoTorneo.Proximo;
            return hoy > FechaFin ? EstadoTorneo.Finalizado : EstadoTorneo.EnCurso;
        }
    }
}
