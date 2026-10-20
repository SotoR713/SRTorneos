namespace SRTorneos.Models;

/// <summary>Equipo inscrito en un torneo (tabla Inscripciones).</summary>
public class EquipoInscrito
{
    public int IdInscripcion { get; set; }
    public int IdEquipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public int IdEstado { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int IdPuesto { get; set; }
    public string Puesto { get; set; } = string.Empty;

    public int Jugadores { get; set; }
    public int Partidos { get; set; }
}
