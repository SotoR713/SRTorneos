namespace SRTorneos.Models;

/// <summary>Números de un jugador dentro de un torneo (se calculan con Anotacion, Faltas y Partidos).</summary>
public class EstadisticaJugador
{
    public int IdJugador { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Numero { get; set; }
    public string Posicion { get; set; } = string.Empty;
    public int IdEquipo { get; set; }
    public string Equipo { get; set; } = string.Empty;

    /// <summary>Partidos con resultado de su equipo (la base no registra asistencia individual).</summary>
    public int PartidosEquipo { get; set; }

    /// <summary>Partidos en los que anotó al menos una vez.</summary>
    public int PartidosAnotando { get; set; }

    public int Faltas { get; set; }

    /// <summary>Valor de la anotación (3, 2, 1…) → cuántas hizo.</summary>
    public Dictionary<int, int> Anotaciones { get; set; } = [];

    public int Cantidad(int valor) => Anotaciones.GetValueOrDefault(valor);
    public int Puntos => Anotaciones.Sum(a => a.Key * a.Value);
    public decimal Promedio => PartidosEquipo == 0 ? 0 : Math.Round((decimal)Puntos / PartidosEquipo, 1);
    public decimal FaltasPorPartido => PartidosEquipo == 0 ? 0 : Math.Round((decimal)Faltas / PartidosEquipo, 1);
}

/// <summary>Lo que hizo un jugador en un partido.</summary>
public class LineaJugadorPartido
{
    public int IdPartido { get; set; }
    public int Faltas { get; set; }
    public Dictionary<int, int> Anotaciones { get; set; } = [];

    public int Cantidad(int valor) => Anotaciones.GetValueOrDefault(valor);
    public int Puntos => Anotaciones.Sum(a => a.Key * a.Value);
}
