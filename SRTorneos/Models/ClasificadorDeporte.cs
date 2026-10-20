namespace SRTorneos.Models;

/// <summary>Familia de deporte a la que pertenece un registro de la tabla Deportes (por su nombre).</summary>
public enum DeporteBase
{
    Basquetbol,
    Futbol,
    FutbolAmericano,
    Voleibol,
    Beisbol,
    Tenis,
    Otro
}

public static class ClasificadorDeporte
{
    public static DeporteBase Clasificar(string? deporte)
    {
        string nombre = (deporte ?? string.Empty).Trim().ToLowerInvariant();
        if (nombre.Contains("americano")) return DeporteBase.FutbolAmericano;
        if (nombre.StartsWith("basq") || nombre.StartsWith("básq") || nombre.StartsWith("basket") || nombre.StartsWith("balonc")) return DeporteBase.Basquetbol;
        if (nombre.StartsWith("fut") || nombre.StartsWith("fút") || nombre.StartsWith("soccer")) return DeporteBase.Futbol;
        if (nombre.StartsWith("vol") || nombre.StartsWith("vól")) return DeporteBase.Voleibol;
        if (nombre.StartsWith("béis") || nombre.StartsWith("beis") || nombre.StartsWith("base")) return DeporteBase.Beisbol;
        if (nombre.StartsWith("ten")) return DeporteBase.Tenis;
        return DeporteBase.Otro;
    }

    /// <summary>En básquetbol, voleibol, béisbol y tenis siempre hay un ganador.</summary>
    public static bool PermiteEmpate(string? deporte) => Clasificar(deporte) is DeporteBase.Futbol or DeporteBase.FutbolAmericano or DeporteBase.Otro;
}
