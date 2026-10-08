namespace SRTorneos.Models;

public class PartidoResumen
{
    public int Id { get; set; }
    public int IdTorneo { get; set; }
    public DateTime Fecha { get; set; }
    public int IdEquipo1 { get; set; }
    public string Equipo1 { get; set; } = string.Empty;
    public int IdEquipo2 { get; set; }
    public string Equipo2 { get; set; } = string.Empty;
    public int IdLugar { get; set; }
    public string Lugar { get; set; } = string.Empty;
    public int? Puntos1 { get; set; }
    public int? Puntos2 { get; set; }
    public int Faltas { get; set; }

    public bool Jugado => Puntos1.HasValue && Puntos2.HasValue;
    public bool GanoLocal => Jugado && Puntos1 > Puntos2;
    public bool GanoVisitante => Jugado && Puntos2 > Puntos1;
}
