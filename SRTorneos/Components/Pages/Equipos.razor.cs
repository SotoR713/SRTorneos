using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class EquiposBase : PaginaBase
{
    [Inject] protected IEquipoRepository EquipoRepository { get; set; } = default!;


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
            FormEquipo = editar is null ? new Equipo() : new Equipo { Id = editar.Id, Nombre = editar.Nombre, Clave = editar.Clave };
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

        // El nombre se puede repetir (p. ej. «Cuervos» en dos torneos); la clave no.
        var ocupadas = Lista.Where(e => e.Id != equipo.Id).Select(e => e.Clave).ToList();
        if (!AsignarClave(equipo, ocupadas)) return;

        if (equipo.Id > 0)
        {
            if (await IntentarAsync(() => EquipoRepository.ActualizarAsync(equipo), "No se pudo actualizar el equipo."))
                IrA("equipos", "equipo-actualizado");
            return;
        }

        if (await IntentarAsync(() => EquipoRepository.CrearAsync(equipo.Nombre, equipo.Clave!), "No se pudo crear el equipo."))
            IrA("equipos", "equipo-creado");
    }

    /// <summary>Clave escrita (validada) o, si viene vacía, una propuesta a partir del nombre.</summary>
    private bool AsignarClave(Equipo equipo, IReadOnlyCollection<string> ocupadas)
    {
        string clave = ClaveEquipo.Normalizar(equipo.Clave);
        if (string.IsNullOrWhiteSpace(equipo.Clave))
        {
            clave = ClaveEquipo.Proponer(equipo.Nombre, ocupadas);
        }
        else if (clave != equipo.Clave!.Trim().ToUpperInvariant())
        {
            Error = "La clave solo puede llevar letras sin acento y números (máximo 3).";
            return false;
        }

        if (ocupadas.Contains(clave, StringComparer.OrdinalIgnoreCase))
        {
            Error = $"La clave «{clave}» ya la usa otro equipo. Prueba con «{ClaveEquipo.Proponer(equipo.Nombre, ocupadas)}».";
            return false;
        }

        equipo.Clave = clave;
        return true;
    }

    protected async Task EliminarAsync()
    {
        if (Lista.All(e => e.Id != EliminarId))
        {
            Error = "El registro ya no existe o ya fue eliminado. Recarga la página.";
            return;
        }

        if (await IntentarAsync(() => EquipoRepository.EliminarAsync(EliminarId),
                "No se pudo eliminar el equipo.",
                "No se puede eliminar: el equipo tiene inscripciones, partidos o resultados registrados."))
        {
            IrA("equipos", "equipo-eliminado");
        }
    }
}
