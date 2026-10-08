using SRTorneos.Components;
using SRTorneos.Data;
using SRTorneos.Repositories;

namespace SRTorneos
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Blazor con renderizado estático en servidor (SSR): sin circuitos SignalR.
            builder.Services.AddRazorComponents();

            builder.Services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
            builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
            builder.Services.AddScoped<ITorneoRepository, TorneoRepository>();
            builder.Services.AddScoped<IEquipoRepository, EquipoRepository>();
            builder.Services.AddScoped<IJugadorRepository, JugadorRepository>();
            builder.Services.AddScoped<IPartidoRepository, PartidoRepository>();
            builder.Services.AddScoped<ILugarRepository, LugarRepository>();

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

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>();

            app.Run();
        }
    }
}
