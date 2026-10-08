using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class TorneoEditorBase : PaginaBase
{
    [Inject] protected ITorneoRepository TorneoRepository { get; set; } = default!;
    [Inject] protected ICatalogoRepository CatalogoRepository { get; set; } = default!;

    /// <summary>Nulo en /torneos/nuevo; con valor en /torneos/{Id}/editar.</summary>
    [Parameter] public int? Id { get; set; }

    [SupplyParameterFromForm(FormName = "torneo-editor")]
    protected TorneoFormulario? Formulario { get; set; }

    protected List<Deporte> Deportes { get; private set; } = [];
    protected List<UsuarioResumen> Usuarios { get; private set; } = [];
    protected List<TipoTorneo> TiposTorneo { get; private set; } = [];

    private TorneoResumen? _torneo;

    protected bool EsEdicion => Id.HasValue;
    protected string Titulo => EsEdicion ? "Editar torneo" : "Crear torneo";
    protected string UrlRegreso => EsEdicion ? $"torneos/{Id}" : "torneos";
    protected bool FaltanCatalogos => Deportes.Count == 0 || TiposTorneo.Count == 0 || Usuarios.Count == 0;

    protected override async Task OnInitializedAsync()
    {
        bool cargado = await IntentarAsync(async () =>
        {
            Deportes = await CatalogoRepository.ObtenerDeportesAsync();
            Usuarios = await CatalogoRepository.ObtenerUsuariosAsync();
            TiposTorneo = await CatalogoRepository.ObtenerTiposTorneoAsync();

            if (EsEdicion)
            {
                _torneo = await TorneoRepository.ObtenerResumenAsync(Id!.Value);
            }
        }, ErrorConexion);

        if (!cargado)
        {
            Formulario = null;
            return;
        }

        if (EsEdicion && _torneo is null)
        {
            Navegacion.NotFound();
            return;
        }

        // En GET el formulario llega nulo: se crea vacío o con los datos actuales del torneo.
        Formulario ??= _torneo is null ? new TorneoFormulario() : TorneoFormulario.Desde(_torneo);
    }

    protected async Task GuardarAsync()
    {
        if (Formulario is null) return;

        if (Deportes.All(d => d.Id != Formulario.IdDeporte)
            || TiposTorneo.All(t => t.Id != Formulario.IdTipoTorneo)
            || Usuarios.All(u => u.Id != Formulario.IdUsuario))
        {
            Error = "Selecciona un deporte, tipo de torneo y creador válidos.";
            return;
        }

        if (_torneo is not null && Formulario.EquiposMax < _torneo.Equipos)
        {
            Error = $"El cupo no puede ser menor a los {_torneo.Equipos} equipos que ya están inscritos.";
            return;
        }

        if (_torneo?.PrimerPartido is DateTime primero && _torneo.UltimoPartido is DateTime ultimo
            && (DateOnly.FromDateTime(primero) < Formulario.FechaInicio || DateOnly.FromDateTime(ultimo) > Formulario.FechaFin))
        {
            Error = $"Ya hay partidos programados del {Presentacion.Fecha(DateOnly.FromDateTime(primero))} al {Presentacion.Fecha(DateOnly.FromDateTime(ultimo))}; las fechas del torneo deben incluirlos.";
            return;
        }

        if (EsEdicion)
        {
            bool actualizado = false;
            if (await IntentarAsync(async () => actualizado = await TorneoRepository.ActualizarAsync(
                    Formulario.ATorneo(Id!.Value), Formulario.PuntosVictoria, Formulario.EquiposMax),
                "No se pudo guardar el torneo. Intenta de nuevo."))
            {
                if (actualizado) IrA($"torneos/{Id}", "torneo-actualizado");
                else Navegacion.NotFound();
            }

            return;
        }

        int nuevoId = 0;
        if (await IntentarAsync(async () => nuevoId = await TorneoRepository.CrearAsync(
                Formulario.ATorneo(), Formulario.PuntosVictoria, Formulario.EquiposMax),
            "No se pudo crear el torneo. Intenta de nuevo."))
        {
            IrA($"torneos/{nuevoId}", "torneo-creado");
        }
    }
}
