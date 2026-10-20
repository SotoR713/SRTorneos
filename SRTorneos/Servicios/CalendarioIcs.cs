using System.Globalization;
using System.Text;
using SRTorneos.Models;

namespace SRTorneos.Servicios;

/// <summary>
/// Genera archivos .ics (iCalendar) para agregar partidos al calendario del celular o de Outlook/Google.
/// Las horas van "flotantes" (sin zona horaria): el calendario las muestra tal como se capturaron.
/// </summary>
public static class CalendarioIcs
{
    public const string TipoContenido = "text/calendar; charset=utf-8";
    private const int DuracionMinutos = 90;

    public static string Generar(string nombreCalendario, IEnumerable<PartidoResumen> partidos, Func<PartidoResumen, string> urlPartido)
    {
        var texto = new StringBuilder();
        void Linea(string linea) => Doblar(texto, linea);

        Linea("BEGIN:VCALENDAR");
        Linea("VERSION:2.0");
        Linea("PRODID:-//SRTorneos//Torneos//ES");
        Linea("CALSCALE:GREGORIAN");
        Linea("METHOD:PUBLISH");
        Linea("X-WR-CALNAME:" + Escapar(nombreCalendario));

        string sello = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        foreach (var partido in partidos)
        {
            string url = urlPartido(partido);
            string resumen = partido.Jugado
                ? $"{partido.Equipo1} {partido.Puntos1}–{partido.Puntos2} {partido.Equipo2}"
                : $"{partido.Equipo1} vs {partido.Equipo2}";

            Linea("BEGIN:VEVENT");
            Linea($"UID:partido-{partido.Id}@srtorneos");
            Linea("DTSTAMP:" + sello);
            Linea("DTSTART:" + partido.Fecha.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture));
            Linea("DTEND:" + partido.Fecha.AddMinutes(DuracionMinutos).ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture));
            Linea("SUMMARY:" + Escapar(resumen));
            Linea("LOCATION:" + Escapar(partido.Lugar));
            Linea("DESCRIPTION:" + Escapar($"{partido.Torneo}\n{url}"));
            Linea("URL:" + url);
            Linea("END:VEVENT");
        }

        Linea("END:VCALENDAR");
        return texto.ToString();
    }

    /// <summary>Nombre de archivo seguro ("Liga Verano 2026" → "liga-verano-2026.ics").</summary>
    public static string NombreArchivo(string nombre)
    {
        var limpio = new StringBuilder();
        foreach (char c in nombre.Normalize(NormalizationForm.FormD).ToLowerInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            limpio.Append(char.IsAsciiLetterOrDigit(c) ? c : '-');
        }

        string resultado = string.Join('-', limpio.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return (resultado.Length == 0 ? "calendario" : resultado[..Math.Min(resultado.Length, 60)]) + ".ics";
    }

    private static string Escapar(string valor)
        => valor.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", "").Replace("\n", "\\n");

    /// <summary>RFC 5545: líneas de máximo 75 bytes; las siguientes empiezan con un espacio.</summary>
    private static void Doblar(StringBuilder destino, string linea)
    {
        int bytes = 0;
        var elementos = StringInfo.GetTextElementEnumerator(linea);
        while (elementos.MoveNext())
        {
            string elemento = elementos.GetTextElement();
            int largo = Encoding.UTF8.GetByteCount(elemento);
            if (bytes + largo > 75)
            {
                destino.Append("\r\n ");
                bytes = 1;
            }

            destino.Append(elemento);
            bytes += largo;
        }

        destino.Append("\r\n");
    }
}
