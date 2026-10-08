namespace SRTorneos.Models;

/// <summary>Elemento de un catálogo simple con el número de registros que lo usan.</summary>
public class ElementoCatalogoUso
{
    public int Id { get; set; }

    /// <summary>Valor tal como está en la base (en Puestos es un número).</summary>
    public string Valor { get; set; } = string.Empty;

    public int Usos { get; set; }
}
