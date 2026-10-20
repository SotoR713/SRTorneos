namespace SRTorneos.Components;

/// <summary>
/// Torneos que sigue el visitante. Se guardan en una cookie de su navegador (no en la base de datos),
/// la escribe el botón "Seguir" con JavaScript (ver App.razor) y aquí solo se lee.
/// </summary>
public static class Seguimiento
{
    public const string Cookie = "srt_sigo";

    public static HashSet<int> Leer(HttpContext? contexto)
    {
        string? valor = contexto?.Request.Cookies[Cookie];
        if (string.IsNullOrEmpty(valor)) return [];

        return valor.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => int.TryParse(v, out int id) ? id : 0)
            .Where(id => id > 0)
            .Take(100)
            .ToHashSet();
    }
}
