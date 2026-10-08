namespace SRTorneos.Models;

public class LugarResumen
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public int Partidos { get; set; }
}
