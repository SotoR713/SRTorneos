using Microsoft.AspNetCore.Components;
using Microsoft.Data.SqlClient;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class NuevoTorneoBase : ComponentBase
{
    [Inject] protected ITorneoRepository TorneoRepository { get; set; } = default!;
    [Inject] protected ICatalogoRepository CatalogoRepository { get; set; } = default!;
    [Inject] protected NavigationManager Navegacion { get; set; } = default!;

    protected TorneoFormulario Formulario { get; } = new();
    protected List<Deporte> Deportes { get; private set; } = [];
    protected List<UsuarioResumen> Usuarios { get; private set; } = [];
    protected List<TipoTorneo> TiposTorneo { get; private set; } = [];
    protected string? Mensaje { get; private set; }
    protected string? DetalleError { get; private set; }
    protected bool CargandoCatalogos { get; private set; } = true;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Deportes = await CatalogoRepository.ObtenerDeportesAsync();
            Usuarios = await CatalogoRepository.ObtenerUsuariosAsync();
            TiposTorneo = await CatalogoRepository.ObtenerTiposTorneoAsync();
        }
        catch (SqlException)
        {
            Mensaje = "No se pudo abrir SRTorneosBD. Verifica que SQL Server esté iniciado, que la base exista y que localhost sea la instancia correcta.";
        }
        finally
        {
            CargandoCatalogos = false;
        }
    }

    protected async Task GuardarAsync()
    {
        Mensaje = null;
        DetalleError = null;

        if (Formulario.FechaFin < Formulario.FechaInicio)
        {
            Mensaje = "La fecha de fin debe ser igual o posterior a la fecha de inicio.";
            return;
        }

        try
        {
            await TorneoRepository.CrearAsync(new Torneo
            {
                Nombre = Formulario.Nombre.Trim(),
                FechaInicio = Formulario.FechaInicio,
                FechaFin = Formulario.FechaFin,
                IdDeporte = Formulario.IdDeporte,
                IdUsuario = Formulario.IdUsuario
                ,IdTipoTorneo = Formulario.IdTipoTorneo
            });
            Navegacion.NavigateTo("torneos");
        }
        catch (SqlException ex)
        {
            Mensaje = "No se pudo guardar el torneo.";
            DetalleError = ex.Message;
        }
    }
}
