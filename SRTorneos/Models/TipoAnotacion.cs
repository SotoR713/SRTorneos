namespace SRTorneos.Models;

/// <summary>Una forma de anotar en un deporte (p. ej. triple = 3 puntos). Cada anotación se guarda como una fila en Anotacion.</summary>
public sealed record TipoAnotacion(int Valor, string Nombre, string Abreviatura);

/// <summary>Cantidad de anotaciones de un mismo valor hechas por un jugador en un partido.</summary>
public sealed record CanastasJugador(int IdJugador, int Valor, int Cantidad);
