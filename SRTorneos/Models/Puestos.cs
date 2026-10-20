using System.Globalization;
using System.Text;

namespace SRTorneos.Models;

/// <summary>
/// Reglas del catálogo Puestos (Puestos.Nombre es texto). Al inscribir se propone el puesto «Inscrito»
/// (o uno equivalente: Inscrita, Participante, Activo); los demás puestos son lugares finales.
/// </summary>
public static class Puestos
{
    public const string Inscrito = "Inscrito";

    private static readonly string[] Iniciales = ["inscrito", "inscrita", "participante", "activo", "sin definir", "0"];

    /// <summary>¿Es el puesto que se tiene al inscribirse (todavía no es un lugar final)?</summary>
    public static bool EsInicial(string? nombre) => Iniciales.Contains(Normalizar(nombre));

    /// <summary>
    /// Texto para mostrar. Los puestos que quedaron como número al pasar la columna a texto se presentan
    /// como antes: "0" → "Sin definir", "1" → "1°".
    /// </summary>
    public static string Mostrar(string? nombre)
    {
        string texto = (nombre ?? string.Empty).Trim();
        if (int.TryParse(texto, out int numero)) return numero <= 0 ? "Sin definir" : $"{numero}°";
        return texto.Length == 0 ? "Sin definir" : texto;
    }

    /// <summary>Puesto que se propone al inscribir: «Inscrito» o equivalente; si no existe, el primero del catálogo.</summary>
    public static int PorDefecto(IReadOnlyList<ElementoCatalogo> puestos)
        => (puestos.FirstOrDefault(p => EsInicial(p.Nombre)) ?? puestos.FirstOrDefault())?.Id ?? 0;

    private static string Normalizar(string? texto)
    {
        var limpio = new StringBuilder();
        foreach (char c in (texto ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) limpio.Append(c);
        }

        return limpio.ToString();
    }
}
