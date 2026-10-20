using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SRTorneos.Components;
using SRTorneos.Data;
using SRTorneos.Repositories;
using SRTorneos.Servicios;

namespace SRTorneos
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Blazor con renderizado estático en servidor (SSR): sin circuitos SignalR.
            builder.Services.AddRazorComponents();

            // Inicio de sesión con cookie. El público puede consultar; solo usuarios registrados modifican.
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(opciones =>
                {
                    opciones.LoginPath = "/login";
                    opciones.AccessDeniedPath = "/login";
                    opciones.Cookie.Name = "SRTorneos.Sesion";
                    opciones.Cookie.HttpOnly = true;
                    opciones.Cookie.SameSite = SameSiteMode.Lax;
                    // Con HTTPS la cookie viaja solo cifrada; en HTTP local (desarrollo) también funciona.
                    opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
                    opciones.SlidingExpiration = true;

                    // Si el usuario se elimina o cambia su contraseña, sus sesiones abiertas dejan de valer.
                    opciones.Events.OnValidatePrincipal = async contexto =>
                    {
                        var cuentas = contexto.HttpContext.RequestServices.GetRequiredService<ServicioCuentas>();
                        bool vigente;
                        try { vigente = await cuentas.SesionVigenteAsync(contexto.Principal!); }
                        catch (Exception) { vigente = true; } // si la base no responde, no se expulsa a nadie
                        if (!vigente)
                        {
                            contexto.RejectPrincipal();
                            await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                    };
                });
            builder.Services.AddAuthorization();

            // Máximo 10 intentos de inicio de sesión por minuto desde la misma dirección.
            builder.Services.AddRateLimiter(opciones =>
            {
                opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                opciones.OnRejected = async (contexto, cancelacion) =>
                {
                    var paginas = contexto.HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>();
                    if (paginas is not null) paginas.Enabled = false;
                    contexto.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                    await contexto.HttpContext.Response.WriteAsync("Demasiados intentos de inicio de sesión. Espera un minuto y vuelve a intentar.", cancelacion);
                };
                opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
                    HttpMethods.IsPost(contexto.Request.Method) && contexto.Request.Path.StartsWithSegments("/login")
                        ? RateLimitPartition.GetFixedWindowLimiter(
                            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) })
                        : RateLimitPartition.GetNoLimiter("libre"));
            });
            builder.Services.AddCascadingAuthenticationState();

            builder.Services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
            builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
            builder.Services.AddScoped<ITorneoRepository, TorneoRepository>();
            builder.Services.AddScoped<IEquipoRepository, EquipoRepository>();
            builder.Services.AddScoped<IJugadorRepository, JugadorRepository>();
            builder.Services.AddScoped<IPartidoRepository, PartidoRepository>();
            builder.Services.AddScoped<ILugarRepository, LugarRepository>();
            builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            builder.Services.AddScoped<ServicioCuentas>();
            builder.Services.AddSingleton<ReglasAnotacion>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            // Fechas y números en español de México.
            app.UseRequestLocalization("es-MX");

            app.UseRateLimiter();
            app.UseAuthentication();

            // Todo cambio de datos en esta aplicación es un POST: sin sesión, se manda a iniciar sesión.
            app.Use(async (contexto, siguiente) =>
            {
                if (HttpMethods.IsPost(contexto.Request.Method)
                    && contexto.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>() is null
                    && contexto.User.Identity?.IsAuthenticated != true
                    && !contexto.Request.Path.StartsWithSegments("/login"))
                {
                    string regreso = contexto.Request.Path + contexto.Request.QueryString;
                    contexto.Response.Redirect($"/login?ReturnUrl={Uri.EscapeDataString(regreso)}");
                    return;
                }

                await siguiente(contexto);
            });

            app.UseAuthorization();
            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>();

            // Calendarios .ics (consulta pública): todo el torneo o un solo partido.
            app.MapGet("/torneos/{id:int}/calendario.ics", async (int id, HttpContext contexto, ITorneoRepository torneos, IPartidoRepository partidos) =>
            {
                var torneo = await torneos.ObtenerResumenAsync(id);
                if (torneo is null) return Results.NotFound();
                var lista = await partidos.ObtenerPorTorneoAsync(id);
                string ics = CalendarioIcs.Generar(torneo.Nombre, lista, p => UrlAbsoluta(contexto, $"torneos/{p.IdTorneo}/partidos/{p.Id}"));
                return Results.File(System.Text.Encoding.UTF8.GetBytes(ics), CalendarioIcs.TipoContenido, CalendarioIcs.NombreArchivo(torneo.Nombre));
            });

            app.MapGet("/torneos/{id:int}/partidos/{idPartido:int}/calendario.ics", async (int id, int idPartido, HttpContext contexto, IPartidoRepository partidos) =>
            {
                var partido = await partidos.ObtenerAsync(id, idPartido);
                if (partido is null) return Results.NotFound();
                string ics = CalendarioIcs.Generar(partido.Torneo, [partido], p => UrlAbsoluta(contexto, $"torneos/{p.IdTorneo}/partidos/{p.Id}"));
                return Results.File(System.Text.Encoding.UTF8.GetBytes(ics), CalendarioIcs.TipoContenido,
                    CalendarioIcs.NombreArchivo($"{partido.Equipo1} vs {partido.Equipo2}"));
            });

            app.MapPost("/logout", async (HttpContext contexto, IAntiforgery antiforgery) =>
            {
                if (!await antiforgery.IsRequestValidAsync(contexto)) return Results.BadRequest();
                await contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/");
            }).RequireAuthorization().DisableAntiforgery();

            app.Run();
        }

        private static string UrlAbsoluta(HttpContext contexto, string ruta)
            => $"{contexto.Request.Scheme}://{contexto.Request.Host}{contexto.Request.PathBase}/{ruta}";
    }
}
