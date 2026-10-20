namespace SRTorneos.Models;

public class Anotador
{
    public int IdJugador { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Numero { get; set; }
    public string Equipo { get; set; } = string.Empty;
    public string EquipoClave { get; set; } = string.Empty;
    public int Puntos { get; set; }
    public int Partidos { get; set; }

    public decimal Promedio => Partidos == 0 ? 0 : Math.Round((decimal)Puntos / Partidos, 1);
}
