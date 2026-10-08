using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class Jugador
{
    public int Id { get; set; }

    [Range(0, 999, ErrorMessage = "El número debe estar entre 0 y 999.")]
    public int Numero { get; set; }

    [Required(ErrorMessage = "El nombre del jugador es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La posición es obligatoria.")]
    [StringLength(20, ErrorMessage = "La posición no puede superar 20 caracteres.")]
    public string Posicion { get; set; } = string.Empty;
}
