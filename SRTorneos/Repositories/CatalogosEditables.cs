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
    bool OrdenPorId,
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
            "(SELECT COUNT(*) FROM ConfigTorn x WHERE x.IdTipoTorneo = c.Id)",
            "Tipo de competencia (por ahora todos se juegan como liga, todos contra todos)", "torneos"),
        new("estados", "Estados", "estado", "Estados", 10, false,
            "(SELECT COUNT(*) FROM Inscripciones x WHERE x.IdEstado = c.Id)",
            "Situación de un equipo inscrito en un torneo (activo, baja, eliminado…). El primero que registraste se propone al inscribir.", "inscripciones"),
        new("puestos", "Puestos", "puesto", "Puestos", 20, true,
            "(SELECT COUNT(*) FROM Inscripciones x WHERE x.IdPuesto = c.Id)",
            $"Lugar de un equipo en el torneo (Inscrito, Campeón, Subcampeón, 3er lugar…). «{Models.Puestos.Inscrito}» se propone al inscribir.", "inscripciones"),
        new("tipos-falta", "Tipos de falta", "tipo de falta", "TiposFaltas", 15, false,
            "(SELECT COUNT(*) FROM Faltas x WHERE x.IdTipoFalta = c.Id)",
            "Clasificación de las faltas que se registran en los partidos.", "faltas")
    ];

    public static CatalogoDefinicion? Buscar(string? clave)
        => Todos.FirstOrDefault(c => string.Equals(c.Clave, clave, StringComparison.OrdinalIgnoreCase));
}
