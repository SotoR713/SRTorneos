using System.Data;
using Microsoft.Data.SqlClient;

namespace SRTorneos.Data;

/// <summary>
/// Atajos para agregar parámetros tipados y leer columnas por nombre.
/// </summary>
public static class SqlExtensiones
{
    public static void Entero(this SqlParameterCollection parametros, string nombre, int valor)
        => parametros.Add(nombre, SqlDbType.Int).Value = valor;

    public static void Texto(this SqlParameterCollection parametros, string nombre, string valor, int largo)
        => parametros.Add(nombre, SqlDbType.VarChar, largo).Value = valor;

    public static void Fecha(this SqlParameterCollection parametros, string nombre, DateOnly valor)
        => parametros.Add(nombre, SqlDbType.Date).Value = valor.ToDateTime(TimeOnly.MinValue);

    public static void FechaHora(this SqlParameterCollection parametros, string nombre, DateTime valor)
        => parametros.Add(nombre, SqlDbType.DateTime).Value = valor;

    public static int Entero(this SqlDataReader lector, string columna)
        => lector.GetInt32(lector.GetOrdinal(columna));

    public static int? EnteroNulo(this SqlDataReader lector, string columna)
    {
        int indice = lector.GetOrdinal(columna);
        return lector.IsDBNull(indice) ? null : lector.GetInt32(indice);
    }

    public static string Texto(this SqlDataReader lector, string columna)
        => lector.GetString(lector.GetOrdinal(columna));

    public static DateOnly Fecha(this SqlDataReader lector, string columna)
        => DateOnly.FromDateTime(lector.GetDateTime(lector.GetOrdinal(columna)));

    public static DateTime FechaHora(this SqlDataReader lector, string columna)
        => lector.GetDateTime(lector.GetOrdinal(columna));

    public static DateTime? FechaHoraNula(this SqlDataReader lector, string columna)
    {
        int indice = lector.GetOrdinal(columna);
        return lector.IsDBNull(indice) ? null : lector.GetDateTime(indice);
    }

    /// <summary>Error 547: la operación choca con una llave foránea (hay datos relacionados).</summary>
    public static bool EsLlaveForanea(this SqlException ex) => ex.Number == 547;
}
