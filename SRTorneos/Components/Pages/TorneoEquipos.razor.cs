using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class TorneoEquiposBase : TorneoPaginaBase
{
    [Inject] protected IEquipoRepository EquipoRepository { get; set; } = default!;
    [Inject] protected ICatalogoRepository CatalogoRepository { get; set; } = default!;


    [SupplyParameterFromForm(FormName = "inscripcion")]
    protected InscripcionFormulario? FormInscripcion { get; set; }

    [SupplyParameterFromForm(FormName = "quitar-equipo")]
    protected int QuitarIdEquipo { get; set; }

    protected List<EquipoInscrito> Inscritos { get; private set; } = [];
    protected List<Equipo> Disponibles { get; private set; } = [];
    protected List<ElementoCatalogo> Estados { get; private set; } = [];
    protected List<ElementoCatalogo> Puestos { get; private set; } = [];

    protected EquipoInscrito? EquipoEnEdicion =>
        FormInscripcion is { IdInscripcion: > 0 }
            ? Inscritos.FirstOrDefault(e => e.IdInscripcion == FormInscripcion.IdInscripcion)
            : null;

    protected bool EnEdicion => EquipoEnEdicion is not null;
    protected bool FaltanCatalogos => Estados.Count == 0 || Puestos.Count == 0;

    protected override async Task CargarAsync()
    {
        Inscritos = await EquipoRepository.ObtenerInscritosAsync(Id);
        Disponibles = await EquipoRepository.ObtenerNoInscritosAsync(Id);
        Estados = await CatalogoRepository.ObtenerEstadosAsync();
        Puestos = await CatalogoRepository.ObtenerPuestosAsync();

        if (FormInscripcion is null)
        {
            var editar = Inscritos.FirstOrDefault(e => e.IdEquipo == Editar);
            FormInscripcion = editar is null
                ? new InscripcionFormulario
                {
                    // Valores sugeridos: primer estado del catálogo y el puesto «Inscrito».
                    IdEstado = Estados.FirstOrDefault()?.Id ?? 0,
                    IdPuesto = SRTorneos.Models.Puestos.PorDefecto(Puestos)
                }
                : new InscripcionFormulario
                {
                    IdInscripcion = editar.IdInscripcion,
                    IdEquipo = editar.IdEquipo,
                    IdEstado = editar.IdEstado,
                    IdPuesto = editar.IdPuesto
                };
        }
    }

    protected async Task GuardarAsync()
    {
        if (Torneo is null || FormInscripcion is null) return;
        var form = FormInscripcion;

        if (Estados.All(e => e.Id != form.IdEstado) || Puestos.All(p => p.Id != form.IdPuesto))
        {
            Error = "Selecciona un estado y un puesto válidos.";
            return;
        }

        if (form.IdInscripcion > 0)
        {
            if (EquipoEnEdicion is null) { Error = "La inscripción ya no existe."; return; }

            if (await IntentarAsync(() => EquipoRepository.ActualizarInscripcionAsync(form.IdInscripcion, Id, form.IdEstado, form.IdPuesto),
                    "No se pudo actualizar la inscripción."))
            {
                IrA($"torneos/{Id}/equipos", "inscripcion-actualizada");
            }

            return;
        }

        if (Torneo.CupoLleno)
        {
            Error = $"El torneo ya tiene su cupo completo ({Torneo.EquiposMax} equipos).";
            return;
        }

        if (form.IdEquipo > 0)
        {
            if (Disponibles.All(e => e.Id != form.IdEquipo))
            {
                Error = "Ese equipo ya está inscrito o no existe.";
                return;
            }

            if (await IntentarAsync(() => EquipoRepository.InscribirAsync(Id, form.IdEquipo, form.IdEstado, form.IdPuesto),
                    "No se pudo inscribir el equipo."))
            {
                IrA($"torneos/{Id}/equipos", "equipo-inscrito");
            }

            return;
        }

        string nombre = form.NuevoEquipo!.Trim();
        List<string> ocupadas = [];
        if (!await IntentarAsync(async () => ocupadas = await EquipoRepository.ObtenerClavesAsync(), ErrorConexion)) return;

        // El nombre se puede repetir; la clave identifica al equipo y no se repite.
        string clave = string.IsNullOrWhiteSpace(form.NuevaClave)
            ? ClaveEquipo.Proponer(nombre, ocupadas)
            : ClaveEquipo.Normalizar(form.NuevaClave);
        if (!string.IsNullOrWhiteSpace(form.NuevaClave) && clave != form.NuevaClave.Trim().ToUpperInvariant())
        {
            Error = "La clave solo puede llevar letras sin acento y números (máximo 3).";
            return;
        }

        if (ocupadas.Contains(clave, StringComparer.OrdinalIgnoreCase))
        {
            Error = $"La clave «{clave}» ya la usa otro equipo. Prueba con «{ClaveEquipo.Proponer(nombre, ocupadas)}».";
            return;
        }

        if (await IntentarAsync(() => EquipoRepository.CrearEInscribirAsync(Id, nombre, clave, form.IdEstado, form.IdPuesto),
                "No se pudo crear e inscribir el equipo."))
        {
            IrA($"torneos/{Id}/equipos", "equipo-inscrito");
        }
    }

    protected async Task QuitarAsync()
    {
        if (Inscritos.All(e => e.IdEquipo != QuitarIdEquipo))
        {
            Error = "El registro ya no existe o ya fue eliminado. Recarga la página.";
            return;
        }

        bool quitado = false;
        if (!await IntentarAsync(async () => quitado = await EquipoRepository.QuitarDelTorneoAsync(Id, QuitarIdEquipo),
                "No se pudo quitar el equipo del torneo.",
                "No se puede quitar: el equipo tiene estadísticas registradas en este torneo.")) return;

        if (quitado) IrA($"torneos/{Id}/equipos", "equipo-quitado");
        else Error = "No se puede quitar: el equipo ya tiene partidos programados en este torneo. Elimina esos partidos primero.";
    }
}
