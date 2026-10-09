using System.Globalization;
using SRTorneos.Models;

namespace SRTorneos.Components;

/// <summary>Textos, íconos y formatos compartidos por las páginas.</summary>
public static class Presentacion
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

    private static readonly Dictionary<string, string> Avisos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["torneo-creado"] = "Torneo creado. Ahora inscribe a los equipos.",
        ["torneo-actualizado"] = "Cambios del torneo guardados.",
        ["torneo-eliminado"] = "Torneo eliminado.",
        ["equipo-inscrito"] = "Equipo inscrito en el torneo.",
        ["inscripcion-actualizada"] = "Inscripción actualizada.",
        ["equipo-quitado"] = "Equipo quitado del torneo.",
        ["jugador-agregado"] = "Jugador agregado a la plantilla.",
        ["jugador-actualizado"] = "Datos del jugador actualizados.",
        ["jugador-quitado"] = "Jugador quitado de la plantilla.",
        ["partido-creado"] = "Partido programado.",
        ["partido-actualizado"] = "Partido actualizado.",
        ["partido-eliminado"] = "Partido eliminado.",
        ["resultado-guardado"] = "Resultado guardado. La tabla de posiciones ya está actualizada.",
        ["resultado-borrado"] = "Resultado borrado; el partido vuelve a estar pendiente.",
        ["falta-registrada"] = "Falta registrada.",
        ["falta-eliminada"] = "Falta eliminada.",
        ["equipo-creado"] = "Equipo creado.",
        ["equipo-actualizado"] = "Equipo actualizado.",
        ["equipo-eliminado"] = "Equipo eliminado.",
        ["lugar-creado"] = "Lugar creado.",
        ["lugar-actualizado"] = "Lugar actualizado.",
        ["lugar-eliminado"] = "Lugar eliminado.",
        ["jugador-creado"] = "Jugador creado. Agrégalo a una plantilla desde el equipo dentro del torneo.",
        ["jugador-eliminado"] = "Jugador eliminado.",
        ["catalogo-creado"] = "Registro agregado al catálogo.",
        ["catalogo-actualizado"] = "Catálogo actualizado.",
        ["catalogo-eliminado"] = "Registro eliminado del catálogo."
    };

    public static string? Aviso(string? clave)
        => clave is not null && Avisos.TryGetValue(clave, out string? texto) ? texto : null;

    public static string IconoDeporte(string? deporte) => ClasificadorDeporte.Clasificar(deporte) switch
    {
        DeporteBase.Basquetbol => "🏀",
        DeporteBase.Futbol => "⚽",
        DeporteBase.FutbolAmericano => "🏈",
        DeporteBase.Voleibol => "🏐",
        DeporteBase.Beisbol => "⚾",
        DeporteBase.Tenis => "🎾",
        _ => "🏆"
    };

    /// <summary>Posiciones sugeridas según el deporte (solo sugerencias; el campo es libre).</summary>
    public static IReadOnlyList<string> PosicionesSugeridas(string? deporte) => ClasificadorDeporte.Clasificar(deporte) switch
    {
        DeporteBase.Basquetbol => ["Base", "Escolta", "Alero", "Ala-pívot", "Pívot"],
        DeporteBase.Futbol => ["Portero", "Defensa", "Medio", "Delantero"],
        DeporteBase.Voleibol => ["Armador", "Opuesto", "Central", "Receptor", "Líbero"],
        DeporteBase.Beisbol => ["Pitcher", "Catcher", "Cuadro", "Jardinero"],
        _ => []
    };

    public static string EtiquetaEstado(EstadoTorneo estado) => estado switch
    {
        EstadoTorneo.Proximo => "Próximo",
        EstadoTorneo.EnCurso => "En curso",
        _ => "Finalizado"
    };

    public static string ClaseEstado(EstadoTorneo estado) => estado switch
    {
        EstadoTorneo.Proximo => "estado-proximo",
        EstadoTorneo.EnCurso => "estado-curso",
        _ => "estado-finalizado"
    };

    public static string Fecha(DateOnly fecha) => fecha.ToString("dd MMM yyyy", Cultura);
    public static string FechaCorta(DateTime fecha) => fecha.ToString("ddd dd MMM", Cultura);
    public static string Hora(DateTime fecha) => fecha.ToString("HH:mm", Cultura);
    public static string FechaHora(DateTime fecha)
    {
        string texto = fecha.ToString("dddd dd 'de' MMMM, HH:mm", Cultura);
        return char.ToUpper(texto[0], Cultura) + texto[1..];
    }
    /// <summary>"a", "a y b", "a, b y c".</summary>
    public static string ListaNatural(IEnumerable<string> elementos, string conjuncion = "y")
    {
        var lista = elementos.ToList();
        return lista.Count <= 1 ? string.Concat(lista) : $"{string.Join(", ", lista[..^1])} {conjuncion} {lista[^1]}";
    }

    public static string Puesto(int puesto) => puesto <= 0 ? "Sin definir" : $"{puesto}°";
}
