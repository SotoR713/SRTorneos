namespace SRTorneos.Models;

public class Partido
{
    public int Id { get; set; }
    public int IdTorneo { get; set; }
    public DateTime Fecha { get; set; }
    public int IdEquipo1 { get; set; }
    public int IdEquipo2 { get; set; }
    public int IdLugar { get; set; }
}
