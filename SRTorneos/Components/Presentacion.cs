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
        ["usuario-creado"] = "Usuario creado.",
        ["cambia-contrasena"] = "Tu contraseña era temporal o de una versión anterior. Escribe una nueva para continuar de forma segura.",
        ["usuario-actualizado"] = "Contraseña actualizada.",
        ["usuario-eliminado"] = "Usuario eliminado.",
        ["calendario-generado"] = "Calendario generado. Puedes editar cualquier partido.",
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
    public static string FechaLarga(DateOnly fecha)
    {
        string texto = fecha.ToString("dddd d 'de' MMMM 'de' yyyy", Cultura);
        return char.ToUpper(texto[0], Cultura) + texto[1..];
    }

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


    // ---------- Estilo app: escudos, estados de partido, compartir ----------

    private static readonly HashSet<string> PalabrasMenores = new(StringComparer.OrdinalIgnoreCase)
        { "de", "del", "la", "las", "el", "los", "y", "fc", "cf", "club", "the" };

    /// <summary>Hasta dos letras para el escudo del equipo ("Halcones Rojos" → "HR", "Lakers" → "LA").</summary>
    public static string Iniciales(string? nombre)
    {
        var palabras = (nombre ?? string.Empty)
            .Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => char.IsLetterOrDigit(p[0]))
            .ToList();
        var importantes = palabras.Where(p => !PalabrasMenores.Contains(p)).ToList();
        if (importantes.Count > 0) palabras = importantes;

        string iniciales = palabras.Count switch
        {
            0 => "?",
            1 => palabras[0].Length > 1 ? palabras[0][..2] : palabras[0],
            _ => $"{palabras[0][0]}{palabras[1][0]}"
        };
        return iniciales.ToUpper(Cultura);
    }

    /// <summary>Color fijo por equipo (el mismo nombre siempre da el mismo color). Sin columnas nuevas en la base.</summary>
    public static string ColorEquipo(string? nombre)
    {
        uint hash = 2166136261;
        foreach (char c in (nombre ?? string.Empty).Trim().ToLowerInvariant())
        {
            hash = (hash ^ c) * 16777619;
        }

        int tono = (int)(hash % 360);
        int saturacion = 52 + (int)(hash / 360 % 18);
        return $"hsl({tono} {saturacion}% 38%)";
    }

    public static (string Texto, string Clase) EstadoPartido(PartidoResumen partido)
    {
        if (partido.Jugado) return ("Final", "chip--final");
        var ahora = DateTime.Now;
        if (partido.Fecha <= ahora && ahora < partido.Fecha.AddHours(3)) return ("En juego", "chip--hoy");
        if (partido.Fecha < ahora) return ("Sin resultado", "chip--pendiente");
        return partido.Fecha.Date == ahora.Date ? ("Hoy", "chip--hoy-proximo") : ("Próximo", "chip--proximo");
    }

    /// <summary>"Hoy", "Mañana", "Ayer" o la fecha corta.</summary>
    public static string DiaRelativo(DateTime fecha)
    {
        int dias = (fecha.Date - DateTime.Today).Days;
        return dias switch
        {
            0 => "Hoy",
            1 => "Mañana",
            -1 => "Ayer",
            _ => fecha.ToString(Math.Abs(dias) < 180 ? "ddd d MMM" : "d MMM yyyy", Cultura)
        };
    }

    /// <summary>Encabezado para agrupar partidos por día.</summary>
    public static string EncabezadoDia(DateOnly dia)
    {
        int dias = dia.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
        string fecha = FechaLarga(dia);
        return dias switch { 0 => $"Hoy · {fecha}", 1 => $"Mañana · {fecha}", -1 => $"Ayer · {fecha}", _ => fecha };
    }

    public static string TextoPartido(PartidoResumen partido)
        => partido.Jugado
            ? $"{partido.Equipo1} {partido.Puntos1} – {partido.Puntos2} {partido.Equipo2} · {partido.Torneo}"
            : $"{partido.Equipo1} vs {partido.Equipo2} · {DiaRelativo(partido.Fecha)} {Hora(partido.Fecha)} en {partido.Lugar} · {partido.Torneo}";

    public static string UrlWhatsApp(string texto) => "https://wa.me/?text=" + Uri.EscapeDataString(texto);

    public static string UrlMapa(string nombre, string direccion)
        => "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(
            string.IsNullOrWhiteSpace(direccion) ? nombre : $"{nombre}, {direccion}");

    /// <summary>"Básquetbol · Liga"; sin tipo (torneo sin configuración en ConfigTorn), solo el deporte.</summary>
    public static string DeporteYTipo(TorneoResumen torneo)
        => string.IsNullOrWhiteSpace(torneo.TipoTorneo) ? torneo.Deporte : $"{torneo.Deporte} · {torneo.TipoTorneo}";

    public static string Decimal(decimal valor) => valor.ToString("0.0", Cultura);
}
