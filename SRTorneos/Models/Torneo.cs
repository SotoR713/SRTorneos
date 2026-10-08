namespace SRTorneos.Models;

public class Torneo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int IdDeporte { get; set; }
    public int IdUsuario { get; set; }
    public int IdTipoTorneo { get; set; }
}
