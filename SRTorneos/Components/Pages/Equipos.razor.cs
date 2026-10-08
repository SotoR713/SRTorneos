using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class EquiposBase : PaginaBase
{
    [Inject] protected IEquipoRepository EquipoRepository { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "editar")]
    public int? Editar { get; set; }

    [SupplyParameterFromForm(FormName = "equipo")]
    protected Equipo? FormEquipo { get; set; }

    [SupplyParameterFromForm(FormName = "eliminar-equipo")]
    protected int EliminarId { get; set; }

    protected List<EquipoResumen> Lista { get; private set; } = [];
    protected bool EnEdicion => FormEquipo is { Id: > 0 };

    protected override async Task OnInitializedAsync()
    {
        if (!await IntentarAsync(async () => Lista = await EquipoRepository.ObtenerTodosAsync(), ErrorConexion)) return;

        if (FormEquipo is null)
        {
            var editar = Lista.FirstOrDefault(e => e.Id == Editar);
            FormEquipo = editar is null ? new Equipo() : new Equipo { Id = editar.Id, Nombre = editar.Nombre };
        }
    }

    protected async Task GuardarAsync()
    {
        if (FormEquipo is null) return;
        var equipo = FormEquipo;

        if (equipo.Id > 0 && Lista.All(e => e.Id != equipo.Id))
        {
            Error = "El equipo ya no existe.";
            return;
        }

        if (Lista.Any(e => e.Id != equipo.Id && string.Equals(e.Nombre.Trim(), equipo.Nombre.Trim(), StringComparison.CurrentCultureIgnoreCase)))
        {
            Error = $"Ya existe un equipo llamado «{equipo.Nombre.Trim()}».";
            return;
        }

        if (equipo.Id > 0)
        {
            if (await IntentarAsync(() => EquipoRepository.ActualizarAsync(equipo), "No se pudo actualizar el equipo."))
                IrA("equipos", "equipo-actualizado");
            return;
        }

        if (await IntentarAsync(() => EquipoRepository.CrearAsync(equipo.Nombre), "No se pudo crear el equipo."))
            IrA("equipos", "equipo-creado");
    }

    protected async Task EliminarAsync()
    {
        if (Lista.All(e => e.Id != EliminarId)) return;

        if (await IntentarAsync(() => EquipoRepository.EliminarAsync(EliminarId),
                "No se pudo eliminar el equipo.",
                "No se puede eliminar: el equipo tiene inscripciones, partidos o resultados registrados."))
        {
            IrA("equipos", "equipo-eliminado");
        }
    }
}
