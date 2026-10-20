using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class Equipo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
    [StringLength(20, ErrorMessage = "El nombre no puede superar 20 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Clave de 1 a 3 letras o números (Equipos.Clave). Vacía en el formulario = se propone sola.</summary>
    [StringLength(ClaveEquipo.Largo, ErrorMessage = "La clave es de máximo 3 caracteres.")]
    public string? Clave { get; set; }
}
