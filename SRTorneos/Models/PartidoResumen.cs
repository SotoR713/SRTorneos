namespace SRTorneos.Models;

public class PartidoResumen
{
    public int Id { get; set; }
    public int IdTorneo { get; set; }
    public string Torneo { get; set; } = string.Empty;
    public string Deporte { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int IdEquipo1 { get; set; }
    public string Equipo1 { get; set; } = string.Empty;
    public string Clave1 { get; set; } = string.Empty;
    public int IdEquipo2 { get; set; }
    public string Equipo2 { get; set; } = string.Empty;
    public string Clave2 { get; set; } = string.Empty;
    public int IdLugar { get; set; }
    public string Lugar { get; set; } = string.Empty;
    public int? Puntos1 { get; set; }
    public int? Puntos2 { get; set; }
    public int Faltas { get; set; }

    public bool Jugado => Puntos1.HasValue && Puntos2.HasValue;
    public bool GanoLocal => Jugado && Puntos1 > Puntos2;
    public bool GanoVisitante => Jugado && Puntos2 > Puntos1;

    public bool Juega(int idEquipo) => IdEquipo1 == idEquipo || IdEquipo2 == idEquipo;

    /// <summary>Rival de <paramref name="idEquipo"/> en este partido.</summary>
    public (int IdEquipo, string Nombre, string Clave) Rival(int idEquipo)
        => idEquipo == IdEquipo1 ? (IdEquipo2, Equipo2, Clave2) : (IdEquipo1, Equipo1, Clave1);

    /// <summary>Puntos a favor y en contra desde el punto de vista de <paramref name="idEquipo"/>.</summary>
    public (int AFavor, int EnContra)? MarcadorDe(int idEquipo)
        => !Jugado ? null : idEquipo == IdEquipo1 ? (Puntos1!.Value, Puntos2!.Value) : (Puntos2!.Value, Puntos1!.Value);
}
