using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class TorneoPartidosBase : TorneoPaginaBase
{
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;
    [Inject] protected IEquipoRepository EquipoRepository { get; set; } = default!;
    [Inject] protected ILugarRepository LugarRepository { get; set; } = default!;


    [SupplyParameterFromForm(FormName = "partido")]
    protected PartidoFormulario? FormPartido { get; set; }

    [SupplyParameterFromForm(FormName = "eliminar-partido")]
    protected int EliminarIdPartido { get; set; }

    protected List<PartidoResumen> Partidos { get; private set; } = [];
    protected List<EquipoInscrito> Equipos { get; private set; } = [];
    protected List<LugarResumen> Lugares { get; private set; } = [];

    protected bool EnEdicion => FormPartido is { Id: > 0 };

    /// <summary>Partidos agrupados por día (la base no guarda número de jornada).</summary>
    protected IEnumerable<(string Titulo, List<PartidoResumen> Partidos)> Grupos => Partidos
        .GroupBy(p => DateOnly.FromDateTime(p.Fecha))
        .OrderBy(g => g.Key)
        .Select(g => (Presentacion.EncabezadoDia(g.Key), g.OrderBy(p => p.Fecha).ThenBy(p => p.Id).ToList()));

    protected override async Task CargarAsync()
    {
        Partidos = await PartidoRepository.ObtenerPorTorneoAsync(Id);
        Equipos = await EquipoRepository.ObtenerInscritosAsync(Id);
        Lugares = await LugarRepository.ObtenerTodosAsync();

        if (FormPartido is null)
        {
            var editar = Partidos.FirstOrDefault(p => p.Id == Editar);
            FormPartido = editar is null ? new PartidoFormulario { Fecha = FechaSugerida() } : PartidoFormulario.Desde(editar);
        }
    }

    protected async Task GuardarAsync()
    {
        if (Torneo is null || FormPartido is null) return;
        var form = FormPartido;

        if (Equipos.All(e => e.IdEquipo != form.IdEquipo1) || Equipos.All(e => e.IdEquipo != form.IdEquipo2))
        {
            Error = "Ambos equipos deben estar inscritos en el torneo.";
            return;
        }

        if (Lugares.All(l => l.Id != form.IdLugar))
        {
            Error = "Selecciona un lugar válido.";
            return;
        }

        var dia = DateOnly.FromDateTime(form.Fecha);
        if (dia < Torneo.FechaInicio || dia > Torneo.FechaFin)
        {
            Error = $"La fecha debe estar dentro del torneo ({Presentacion.Fecha(Torneo.FechaInicio)} al {Presentacion.Fecha(Torneo.FechaFin)}).";
            return;
        }

        if (form.Id > 0)
        {
            var actual = Partidos.FirstOrDefault(p => p.Id == form.Id);
            if (actual is null) { Error = "El partido ya no existe."; return; }

            if ((actual.Jugado || actual.Faltas > 0) && (actual.IdEquipo1 != form.IdEquipo1 || actual.IdEquipo2 != form.IdEquipo2))
            {
                Error = "Este partido ya tiene resultado o faltas registradas. Bórralos antes de cambiar los equipos.";
                return;
            }

            if (await IntentarAsync(() => PartidoRepository.ActualizarAsync(form.APartido(Id)), "No se pudo actualizar el partido."))
            {
                IrA($"torneos/{Id}/partidos", "partido-actualizado");
            }

            return;
        }

        if (await IntentarAsync(() => PartidoRepository.CrearAsync(form.APartido(Id)), "No se pudo programar el partido."))
        {
            IrA($"torneos/{Id}/partidos", "partido-creado");
        }
    }

    protected async Task EliminarAsync()
    {
        if (Partidos.All(p => p.Id != EliminarIdPartido))
        {
            Error = "El registro ya no existe o ya fue eliminado. Recarga la página.";
            return;
        }

        if (await IntentarAsync(() => PartidoRepository.EliminarAsync(Id, EliminarIdPartido), "No se pudo eliminar el partido."))
        {
            IrA($"torneos/{Id}/partidos", "partido-eliminado");
        }
    }

    /// <summary>Hoy a las 18:00 si cae dentro del torneo; si no, el primer día del torneo.</summary>
    private DateTime FechaSugerida()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var dia = Torneo is null || (hoy >= Torneo.FechaInicio && hoy <= Torneo.FechaFin) ? hoy : Torneo.FechaInicio;
        return dia.ToDateTime(new TimeOnly(18, 0));
    }
}
