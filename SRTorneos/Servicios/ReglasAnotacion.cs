using SRTorneos.Models;

namespace SRTorneos.Servicios;

/// <summary>
/// Tipos de anotación por deporte, filtrados por los valores que acepta la base de datos.
/// En SRTorneosBD la restricción CK_Anotacion_Puntos decide qué valores se pueden guardar;
/// "Anotaciones:ValoresPermitidos" en appsettings.json debe reflejar esa misma regla.
/// </summary>
public class ReglasAnotacion(IConfiguration configuration)
{
    private readonly HashSet<int> _permitidos =
        (configuration.GetSection("Anotaciones:ValoresPermitidos").Get<int[]>() is { Length: > 0 } valores ? valores : [1, 2, 3])
        .ToHashSet();

    public IReadOnlyCollection<int> ValoresPermitidos => _permitidos;

    public static IReadOnlyList<TipoAnotacion> TiposDelDeporte(string? deporte) => ClasificadorDeporte.Clasificar(deporte) switch
    {
        DeporteBase.Basquetbol => [new(3, "Triples", "3P"), new(2, "Dobles", "2P"), new(1, "Tiros libres", "TL")],
        DeporteBase.Futbol => [new(1, "Goles", "G")],
        DeporteBase.Voleibol => [new(1, "Puntos", "P")],
        DeporteBase.Beisbol => [new(1, "Carreras", "C")],
        _ => [new(1, "Anotaciones", "A")]
    };

    /// <summary>Tipos de anotación que se pueden capturar por jugador (la base los acepta).</summary>
    public IReadOnlyList<TipoAnotacion> Permitidos(string? deporte)
        => TiposDelDeporte(deporte).Where(t => _permitidos.Contains(t.Valor)).ToList();

    /// <summary>Tipos de anotación del deporte que la base no acepta (solo cuentan en el marcador del equipo).</summary>
    public IReadOnlyList<TipoAnotacion> NoPermitidos(string? deporte)
        => TiposDelDeporte(deporte).Where(t => !_permitidos.Contains(t.Valor)).ToList();
}
