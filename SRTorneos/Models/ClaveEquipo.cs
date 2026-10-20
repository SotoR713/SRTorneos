using System.Text;

namespace SRTorneos.Models;

/// <summary>
/// Clave corta del equipo (Equipos.Clave, varchar(3), obligatoria y sin repetirse).
/// Es el identificador visual: dos equipos pueden llamarse igual ("Cuervos") pero con claves distintas (CUR, CVO).
/// </summary>
public static class ClaveEquipo
{
    public const int Largo = 3;

    private static readonly HashSet<string> PalabrasMenores = new(StringComparer.OrdinalIgnoreCase)
        { "de", "del", "la", "las", "el", "los", "y", "fc", "cf", "club" };

    /// <summary>Mayúsculas, sin acentos ni espacios, solo letras y números, máximo 3.</summary>
    public static string Normalizar(string? texto)
    {
        string limpio = SoloLetrasYNumeros(texto);
        return limpio.Length > Largo ? limpio[..Largo] : limpio;
    }

    private static string SoloLetrasYNumeros(string? texto)
    {
        var limpio = new StringBuilder();
        foreach (char c in (texto ?? string.Empty).Normalize(NormalizationForm.FormD).ToUpperInvariant())
        {
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9') limpio.Append(c);
        }

        return limpio.ToString();
    }

    public static bool EsValida(string? clave) => clave is { Length: > 0 and <= Largo } && Normalizar(clave) == clave;

    /// <summary>Propone una clave libre a partir del nombre ("Halcones Rojos" → HAL, HRO, HR1…).</summary>
    public static string Proponer(string nombre, IEnumerable<string> ocupadas)
    {
        var usadas = ocupadas.Select(c => c.Trim().ToUpperInvariant()).ToHashSet();
        var palabras = nombre.Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(SoloLetrasYNumeros)
            .Where(p => p.Length > 0)
            .ToList();
        var importantes = palabras.Where(p => !PalabrasMenores.Contains(p)).ToList();
        if (importantes.Count > 0) palabras = importantes;
        if (palabras.Count == 0) palabras = ["EQ"];

        string principal = string.Concat(palabras);
        var candidatos = new List<string>
        {
            Normalizar(palabras[0]),
            Normalizar(string.Concat(palabras.Take(3).Select(p => p[0]))),
            Normalizar(palabras[0][..1] + (palabras.Count > 1 ? palabras[1] : palabras[0][1..])),
            Normalizar(principal[..1] + new string(principal.Skip(1).Where(c => "AEIOU".IndexOf(c) < 0).ToArray()))
        };

        foreach (string candidato in candidatos.Where(c => c.Length == Largo))
        {
            if (!usadas.Contains(candidato)) return candidato;
        }

        string base2 = Normalizar(palabras[0]).PadRight(2, 'X')[..2];
        for (int i = 1; i <= 9; i++)
        {
            if (!usadas.Contains($"{base2}{i}")) return $"{base2}{i}";
        }

        for (int i = 10; i <= 99; i++)
        {
            if (!usadas.Contains($"{base2[0]}{i}")) return $"{base2[0]}{i}";
        }

        return "EQ" + Random.Shared.Next(0, 10);
    }
}
