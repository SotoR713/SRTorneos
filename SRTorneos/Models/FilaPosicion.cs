namespace SRTorneos.Models;

public class FilaPosicion
{
    public int IdEquipo { get; set; }
    public string Equipo { get; set; } = string.Empty;
    public int Jugados { get; set; }
    public int Ganados { get; set; }
    public int Perdidos { get; set; }
    public int Empatados { get; set; }
    public int PuntosFavor { get; set; }
    public int PuntosContra { get; set; }
    public int Puntos { get; set; }

    public int Diferencia => PuntosFavor - PuntosContra;
}
