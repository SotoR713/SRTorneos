namespace SRTorneos.Models;

/// <summary>Jugador alineado en un equipo dentro de un torneo, con sus estadísticas.</summary>
public class JugadorPlantilla
{
    public int IdAlineacion { get; set; }
    public int IdEquipo { get; set; }
    public int IdJugador { get; set; }
    public int Numero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Posicion { get; set; } = string.Empty;
    public int Puntos { get; set; }
    public int Faltas { get; set; }
}
