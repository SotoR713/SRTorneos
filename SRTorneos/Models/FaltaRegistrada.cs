namespace SRTorneos.Models;

public class FaltaRegistrada
{
    public int Id { get; set; }
    public int IdJugador { get; set; }
    public string Jugador { get; set; } = string.Empty;
    public int Numero { get; set; }
    public int IdEquipo { get; set; }
    public string Equipo { get; set; } = string.Empty;
    public string TipoFalta { get; set; } = string.Empty;
}
