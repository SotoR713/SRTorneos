using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;
using SRTorneos.Servicios;

namespace SRTorneos.Components.Pages;

public class GenerarCalendarioBase : TorneoPaginaBase
{
    [Inject] protected IPartidoRepository PartidoRepository { get; set; } = default!;
    [Inject] protected IEquipoRepository EquipoRepository { get; set; } = default!;
    [Inject] protected ILugarRepository LugarRepository { get; set; } = default!;

    [SupplyParameterFromForm(FormName = "generador")]
    protected GeneradorFormulario? FormGenerador { get; set; }

    protected List<EquipoInscrito> Equipos { get; private set; } = [];
    protected List<LugarResumen> TodosLosLugares { get; private set; } = [];
    protected List<PartidoPropuesto> Propuesta { get; private set; } = [];
    protected int LigaSinResultado { get; private set; }
    private int _ligaConResultado;

    protected string NombreEquipo(int id) => Equipos.FirstOrDefault(e => e.IdEquipo == id)?.Nombre ?? "?";
    protected string NombreLugar(int id) => TodosLosLugares.FirstOrDefault(l => l.Id == id)?.Nombre ?? "?";

    /// <summary>Equipo que descansa en la jornada (con número impar de equipos).</summary>
    protected string? Descansa(int jornada)
    {
        var juegan = Propuesta.Where(p => p.Jornada == jornada).SelectMany(p => new[] { p.IdEquipo1, p.IdEquipo2 }).ToHashSet();
        var libre = Equipos.Where(e => !juegan.Contains(e.IdEquipo)).Select(e => e.Nombre).ToList();
        return libre.Count == 0 ? null : string.Join(", ", libre);
    }

    protected override async Task CargarAsync()
    {
        Equipos = await EquipoRepository.ObtenerInscritosAsync(Id);
        TodosLosLugares = await LugarRepository.ObtenerTodosAsync();
        var partidos = await PartidoRepository.ObtenerPorTorneoAsync(Id);
        LigaSinResultado = partidos.Count(p => !p.Jugado);
        _ligaConResultado = partidos.Count(p => p.Jugado);

        if (FormGenerador is null)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            FormGenerador = new GeneradorFormulario
            {
                PrimeraFecha = hoy > Torneo!.FechaInicio && hoy <= Torneo.FechaFin ? hoy : Torneo.FechaInicio,
                Semilla = Random.Shared.Next(1, int.MaxValue),
                Lugares = TodosLosLugares.Select((l, i) => new LugarElegido { IdLugar = l.Id, Elegido = i == 0 }).ToList()
            };
            Previsualizar(mostrarErrores: false);
        }
        else
        {
            // En el POST la lista llega del formulario; se descartan canchas que ya no existen.
            FormGenerador.Lugares = (FormGenerador.Lugares ?? []).Where(l => TodosLosLugares.Any(t => t.Id == l.IdLugar)).ToList();
        }
    }

    protected async Task ProcesarAsync()
    {
        if (FormGenerador is null || Torneo is null) return;

        if (FormGenerador.Accion == "sortear") FormGenerador.Semilla = Random.Shared.Next(1, int.MaxValue);
        if (!Previsualizar(mostrarErrores: true) || FormGenerador.Accion != "generar") return;

        if (_ligaConResultado > 0)
        {
            Error = $"Ya hay {_ligaConResultado} partidos con resultado. El generador es para armar el calendario completo desde cero; programa los partidos restantes a mano.";
            return;
        }

        if (LigaSinResultado > 0 && !FormGenerador.Reemplazar)
        {
            Error = $"Ya hay {LigaSinResultado} partidos programados. Marca «Reemplazar» para sustituirlos por este calendario.";
            return;
        }

        var partidos = Propuesta.Select(p => new Partido
        {
            IdTorneo = Id,
            Fecha = p.Fecha,
            IdEquipo1 = p.IdEquipo1,
            IdEquipo2 = p.IdEquipo2,
            IdLugar = p.IdLugar
        }).ToList();

        if (await IntentarAsync(() => PartidoRepository.CrearVariosAsync(Id, partidos, FormGenerador.Reemplazar), "No se pudo guardar el calendario."))
        {
            IrA($"torneos/{Id}/partidos", "calendario-generado");
        }
    }

    private bool Previsualizar(bool mostrarErrores)
    {
        Propuesta = [];
        var form = FormGenerador!;
        var lugares = (form.Lugares ?? []).Where(l => l.Elegido).Select(l => l.IdLugar).ToList();

        string? problema = null;
        if (Equipos.Count < 2) problema = "Inscribe al menos dos equipos.";
        else if (lugares.Count == 0) problema = "Elige al menos una cancha.";
        else if (form.PrimeraFecha < Torneo!.FechaInicio || form.PrimeraFecha > Torneo.FechaFin)
            problema = $"La primera jornada debe estar entre {Presentacion.Fecha(Torneo.FechaInicio)} y {Presentacion.Fecha(Torneo.FechaFin)}.";

        if (problema is null)
        {
            var parametros = new ParametrosCalendario(form.PrimeraFecha, form.Hora, form.DiasEntreJornadas,
                form.MinutosEntrePartidos, lugares, form.IdaYVuelta, form.Semilla);
            Propuesta = GeneradorCalendario.GenerarLiga(Equipos.Select(e => e.IdEquipo).ToList(), parametros);

            var ultimo = DateOnly.FromDateTime(Propuesta.Max(p => p.Fecha));
            if (ultimo > Torneo!.FechaFin)
            {
                problema = $"La última jornada caería el {Presentacion.Fecha(ultimo)}, después del fin del torneo ({Presentacion.Fecha(Torneo.FechaFin)}). " +
                           "Reduce los días entre jornadas, empieza antes o amplía las fechas del torneo.";
            }
        }

        if (problema is not null && mostrarErrores) Error = problema;
        return problema is null;
    }
}
