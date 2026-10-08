namespace SRTorneos.Models;

public class JugadorResumen
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Posicion { get; set; } = string.Empty;
    public int Torneos { get; set; }
    public int Puntos { get; set; }
    public int Faltas { get; set; }

    public bool SePuedeEliminar => Torneos == 0 && Puntos == 0 && Faltas == 0;
}
