using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class CatalogoFormulario
{
    /// <summary>0 = nuevo; mayor que 0 = edición.</summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "Escribe un valor.")]
    public string Valor { get; set; } = string.Empty;
}
