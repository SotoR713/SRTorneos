using System.ComponentModel.DataAnnotations;

namespace SRTorneos.Models;

/// <summary>Cancha que se puede usar al programar automáticamente.</summary>
public class LugarElegido
{
    public int IdLugar { get; set; }
    public bool Elegido { get; set; }
}

/// <summary>Parámetros del generador de calendario de liga.</summary>
public class GeneradorFormulario
{
    public DateOnly PrimeraFecha { get; set; }
    public TimeOnly Hora { get; set; } = new(18, 0);

    [Range(1, 60, ErrorMessage = "Los días entre jornadas deben estar entre 1 y 60.")]
    public int DiasEntreJornadas { get; set; } = 7;

    [Range(15, 600, ErrorMessage = "Los minutos entre partidos deben estar entre 15 y 600.")]
    public int MinutosEntrePartidos { get; set; } = 90;

    public bool IdaYVuelta { get; set; }
    public bool Reemplazar { get; set; }
    public int Semilla { get; set; }
    public string? Accion { get; set; }
    public List<LugarElegido>? Lugares { get; set; } = [];
}
