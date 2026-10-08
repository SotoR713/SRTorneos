using Microsoft.AspNetCore.Components;
using Microsoft.Data.SqlClient;
using SRTorneos.Data;

namespace SRTorneos.Components;

/// <summary>
/// Base de todas las páginas SSR: avisos por query string (?aviso=clave),
/// manejo uniforme de errores de SQL y redirección después de un POST.
/// </summary>
public abstract class PaginaBase : ComponentBase
{
    protected const string ErrorConexion =
        "No fue posible conectar con SRTorneosBD. Verifica que SQL Server esté iniciado.";

    [Inject] protected NavigationManager Navegacion { get; set; } = default!;
    [Inject] protected ILogger<PaginaBase> Logger { get; set; } = default!;

    [CascadingParameter] public HttpContext? HttpContext { get; set; }

    [SupplyParameterFromQuery(Name = "aviso")]
    public string? ClaveAviso { get; set; }

    /// <summary>El aviso de éxito solo se muestra al llegar por GET (después de la redirección).</summary>
    protected string? Aviso =>
        Error is null && !HttpMethods.IsPost(HttpContext?.Request.Method ?? string.Empty)
            ? Presentacion.Aviso(ClaveAviso)
            : null;

    protected string? Error { get; set; }

    /// <summary>
    /// Ejecuta una operación de base de datos; si falla, registra el error y deja un mensaje amigable.
    /// </summary>
    protected async Task<bool> IntentarAsync(Func<Task> accion, string mensajeError, string? mensajeRelacionados = null)
    {
        try
        {
            await accion();
            return true;
        }
        catch (SqlException ex)
        {
            Logger.LogError(ex, "Error de SQL: {Mensaje}", mensajeError);
            Error = ex.EsLlaveForanea() && mensajeRelacionados is not null ? mensajeRelacionados : mensajeError;
            return false;
        }
    }

    /// <summary>Patrón POST-Redirect-GET con aviso opcional.</summary>
    protected void IrA(string url, string? aviso = null)
        => Navegacion.NavigateTo(aviso is null ? url : $"{url}?aviso={aviso}");
}
