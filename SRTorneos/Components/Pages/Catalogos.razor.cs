using Microsoft.AspNetCore.Components;
using SRTorneos.Models;
using SRTorneos.Repositories;

namespace SRTorneos.Components.Pages;

public class CatalogosBase : PaginaBase
{
    [Inject] protected ICatalogoRepository CatalogoRepository { get; set; } = default!;

    [Parameter] public string? Clave { get; set; }


    [SupplyParameterFromForm(FormName = "catalogo")]
    protected CatalogoFormulario? FormCatalogo { get; set; }

    [SupplyParameterFromForm(FormName = "eliminar-catalogo")]
    protected int EliminarId { get; set; }

    protected CatalogoDefinicion? Definicion { get; private set; }
    protected List<ElementoCatalogoUso> Lista { get; private set; } = [];
    protected bool EnEdicion => FormCatalogo is { Id: > 0 };

    private string UrlCatalogo => $"catalogos/{Definicion!.Clave}";

    protected override async Task OnInitializedAsync()
    {
        Definicion = CatalogosEditables.Buscar(Clave ?? CatalogosEditables.Todos[0].Clave);
        if (Definicion is null)
        {
            Navegacion.NotFound();
            return;
        }

        if (!await IntentarAsync(async () => Lista = await CatalogoRepository.ObtenerConUsoAsync(Definicion), ErrorConexion)) return;

        if (FormCatalogo is null)
        {
            var editar = Lista.FirstOrDefault(e => e.Id == Editar);
            FormCatalogo = editar is null ? new CatalogoFormulario() : new CatalogoFormulario { Id = editar.Id, Valor = editar.Valor };
        }
    }

    protected string Mostrar(string valor)
        => Definicion is { Numerico: true } && int.TryParse(valor, out int numero) ? Presentacion.Puesto(numero) : valor;

    protected async Task GuardarAsync()
    {
        if (Definicion is null || FormCatalogo is null) return;
        var form = FormCatalogo;
        string valor = form.Valor.Trim();

        if (Definicion.Numerico)
        {
            if (!int.TryParse(valor, out int numero) || numero < 0 || numero > 999)
            {
                Error = "El puesto debe ser un número entre 0 y 999.";
                return;
            }

            valor = numero.ToString();
        }
        else if (valor.Length > Definicion.LargoMaximo)
        {
            Error = $"El nombre no puede superar {Definicion.LargoMaximo} caracteres.";
            return;
        }

        if (form.Id > 0 && Lista.All(e => e.Id != form.Id))
        {
            Error = "El registro ya no existe.";
            return;
        }

        if (Lista.Any(e => e.Id != form.Id && string.Equals(e.Valor.Trim(), valor, StringComparison.CurrentCultureIgnoreCase)))
        {
            Error = $"Ya existe «{Mostrar(valor)}» en {Definicion.Titulo.ToLowerInvariant()}.";
            return;
        }

        if (form.Id > 0)
        {
            if (await IntentarAsync(() => CatalogoRepository.ActualizarAsync(Definicion, form.Id, valor), "No se pudo guardar el cambio."))
                IrA(UrlCatalogo, "catalogo-actualizado");
            return;
        }

        if (await IntentarAsync(() => CatalogoRepository.CrearAsync(Definicion, valor), "No se pudo agregar el registro."))
            IrA(UrlCatalogo, "catalogo-creado");
    }

    protected async Task EliminarAsync()
    {
        if (Definicion is null) return;
        if (Lista.All(e => e.Id != EliminarId))
        {
            Error = "El registro ya no existe o ya fue eliminado. Recarga la página.";
            return;
        }

        if (await IntentarAsync(() => CatalogoRepository.EliminarAsync(Definicion, EliminarId),
                "No se pudo eliminar el registro.",
                $"No se puede eliminar: está en uso en {Definicion.DondeSeUsa}."))
        {
            IrA(UrlCatalogo, "catalogo-eliminado");
        }
    }
}
