using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

public class FaltaFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un jugador.")]
    public int IdJugador { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el tipo de falta.")]
    public int IdTipoFalta { get; set; }
}
