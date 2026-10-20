using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class LugaresBase : PaginaBase
{
    [Inject] protected ILugarRepository LugarRepository { get; set; } = default!;


    [SupplyParameterFromForm(FormName = "lugar")]
    protected Lugar? FormLugar { get; set; }

    [SupplyParameterFromForm(FormName = "eliminar-lugar")]
    protected int EliminarId { get; set; }

    protected List<LugarResumen> Lista { get; private set; } = [];
    protected bool EnEdicion => FormLugar is { Id: > 0 };

    protected override async Task OnInitializedAsync()
    {
        if (!await IntentarAsync(async () => Lista = await LugarRepository.ObtenerTodosAsync(), ErrorConexion)) return;

        if (FormLugar is null)
        {
            var editar = Lista.FirstOrDefault(l => l.Id == Editar);
            FormLugar = editar is null ? new Lugar() : new Lugar { Id = editar.Id, Nombre = editar.Nombre, Direccion = editar.Direccion };
        }
    }

    protected async Task GuardarAsync()
    {
        if (FormLugar is null) return;
        var lugar = FormLugar;

        if (lugar.Id > 0)
        {
            if (Lista.All(l => l.Id != lugar.Id)) { Error = "El lugar ya no existe."; return; }

            if (await IntentarAsync(() => LugarRepository.ActualizarAsync(lugar), "No se pudo actualizar el lugar."))
                IrA("lugares", "lugar-actualizado");
            return;
        }

        if (await IntentarAsync(() => LugarRepository.CrearAsync(lugar), "No se pudo crear el lugar."))
            IrA("lugares", "lugar-creado");
    }

    protected async Task EliminarAsync()
    {
        if (Lista.All(l => l.Id != EliminarId))
        {
            Error = "El registro ya no existe o ya fue eliminado. Recarga la página.";
            return;
        }

        if (await IntentarAsync(() => LugarRepository.EliminarAsync(EliminarId),
                "No se pudo eliminar el lugar.",
                "No se puede eliminar: hay partidos programados en este lugar."))
        {
            IrA("lugares", "lugar-eliminado");
        }
    }
}
