namespace SRTorneos.Repositories;

/// <summary>
/// Describe un catálogo simple (Id + Nombre) que se administra desde /catalogos.
/// Los nombres de tabla y las consultas son constantes del código (nunca vienen del usuario).
/// </summary>
public sealed record CatalogoDefinicion(
    string Clave,
    string Titulo,
    string Singular,
    string Tabla,
    int LargoMaximo,
    bool Numerico,
    string ConsultaUsos,
    string Descripcion,
    string DondeSeUsa);

public static class CatalogosEditables
{
    public static readonly IReadOnlyList<CatalogoDefinicion> Todos =
    [
        new("deportes", "Deportes", "deporte", "Deportes", 15, false,
            "(SELECT COUNT(*) FROM Torneos x WHERE x.IdDeporte = c.Id)",
            "Deportes disponibles al crear un torneo.", "torneos"),
        new("tipos-torneo", "Tipos de torneo", "tipo de torneo", "TiposTorneos", 15, false,
            "(SELECT COUNT(*) FROM Torneos x WHERE x.IdTipoTorneo = c.Id)",
            "Formato de la competencia: liga, eliminatoria, grupos…", "torneos"),
        new("estados", "Estados", "estado", "Estados", 10, false,
            "(SELECT COUNT(*) FROM Inscripciones x WHERE x.IdEstado = c.Id)",
            "Situación de un equipo inscrito en un torneo (activo, baja, eliminado…). El primero de la lista se propone al inscribir.", "inscripciones"),
        new("puestos", "Puestos", "puesto", "Puestos", 3, true,
            "(SELECT COUNT(*) FROM Inscripciones x WHERE x.IdPuesto = c.Id)",
            "Lugar final de un equipo en el torneo (1°, 2°…). El 0 significa «Sin definir» y es el que se propone al inscribir.", "inscripciones"),
        new("tipos-falta", "Tipos de falta", "tipo de falta", "TiposFaltas", 15, false,
            "(SELECT COUNT(*) FROM Faltas x WHERE x.IdTipoFalta = c.Id)",
            "Clasificación de las faltas que se registran en los partidos.", "faltas")
    ];

    public static CatalogoDefinicion? Buscar(string? clave)
        => Todos.FirstOrDefault(c => string.Equals(c.Clave, clave, StringComparison.OrdinalIgnoreCase));
}
