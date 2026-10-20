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
    /// <summary>Mensaje genérico al leer datos. Si de verdad no hay conexión, IntentarAsync lo cambia por <see cref="SinConexion"/>.</summary>
    protected const string ErrorConexion = "No se pudieron cargar los datos de SRTorneosBD.";

    private const string SinConexion = "No fue posible conectar con SRTorneosBD. Verifica que SQL Server esté iniciado.";

    /// <summary>Errores de SQL Server que significan "no hay conexión" (servidor apagado, red, base o permiso de acceso).</summary>
    private static bool EsSinConexion(SqlException ex) => ex.Number is -2 or -1 or 2 or 53 or 4060 or 18456 or 10053 or 10054 or 10060 or 10061 or 11001;

    [Inject] protected NavigationManager Navegacion { get; set; } = default!;
    [Inject] protected ILogger<PaginaBase> Logger { get; set; } = default!;
    [Inject] protected IWebHostEnvironment Entorno { get; set; } = default!;

    [CascadingParameter] public HttpContext? HttpContext { get; set; }

    [SupplyParameterFromQuery(Name = "aviso")]
    public string? ClaveAviso { get; set; }

    /// <summary>?editar=Id en las páginas de catálogo. Se recibe como texto para que un valor inválido no tumbe la página.</summary>
    [SupplyParameterFromQuery(Name = "editar")]
    public string? EditarTexto { get; set; }

    protected int? Editar => int.TryParse(EditarTexto, out int id) ? id : null;

    /// <summary>El aviso de éxito solo se muestra al llegar por GET (después de la redirección).</summary>
    protected string? Aviso =>
        Error is null && !HttpMethods.IsPost(HttpContext?.Request.Method ?? string.Empty)
            ? Presentacion.Aviso(ClaveAviso)
            : null;

    protected string? Error { get; set; }

    /// <summary>Hay sesión iniciada: puede capturar y editar. Sin sesión, solo consulta.</summary>
    protected bool EsAdmin => HttpContext?.User.Identity?.IsAuthenticated == true;

    protected int IdUsuarioActual => int.TryParse(HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

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
            Logger.LogError(ex, "Error de SQL {Numero}: {Mensaje}", ex.Number, mensajeError);
            string mensaje = ex switch
            {
                _ when EsSinConexion(ex) => SinConexion,
                _ when ex.EsLlaveForanea() && mensajeRelacionados is not null => mensajeRelacionados,
                _ when ex.EsDuplicado() => $"{mensajeError} Ya existe un registro con ese valor y la base no permite repetirlo.",
                _ when ex.EsRestriccionCheck() => $"{mensajeError} La base de datos rechazó un valor por una de sus reglas (restricción CHECK).",
                // 207 = columna inexistente, 208 = tabla inexistente: la base está desactualizada, no caída.
                _ when ex.Number is 207 or 208 => "La base de datos no tiene una tabla o columna que la aplicación necesita.",
                _ => mensajeError
            };
            Error = mensaje + Detalle(ex);
            return false;
        }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidCastException or FormatException
                                       or IndexOutOfRangeException or OverflowException or System.Data.SqlTypes.SqlTypeException)
        {
            // Errores típicos de ADO.NET (conexión, columnas o tipos que no coinciden con la base).
            Logger.LogError(ex, "Error al acceder a datos: {Mensaje}", mensajeError);
            Error = mensajeError + Detalle(ex);
            return false;
        }
    }

    /// <summary>En desarrollo, y solo a usuarios con sesión, agrega el mensaje técnico para diagnosticar.</summary>
    private string Detalle(Exception ex)
        => Entorno.IsDevelopment() && EsAdmin
            ? $" Detalle técnico: {(ex is SqlException sql ? $"[SQL {sql.Number}] " : $"[{ex.GetType().Name}] ")}{ex.Message}"
            : string.Empty;

    /// <summary>Patrón POST-Redirect-GET con aviso opcional.</summary>
    protected void IrA(string url, string? aviso = null)
        => Navegacion.NavigateTo(aviso is null ? url : $"{url}{(url.Contains('?') ? '&' : '?')}aviso={aviso}");
}
