namespace SRTorneos.Servicios;

public sealed record ParametrosCalendario(
    DateOnly PrimeraFecha,
    TimeOnly Hora,
    int DiasEntreJornadas,
    int MinutosEntrePartidos,
    IReadOnlyList<int> Lugares,
    bool IdaYVuelta,
    int Semilla);

public sealed record PartidoPropuesto(int Jornada, DateTime Fecha, int IdEquipo1, int IdEquipo2, int IdLugar);

/// <summary>
/// Calendario de liga "todos contra todos" con el método del círculo (tabla de Berger):
/// cada pareja se enfrenta una vez (o dos con ida y vuelta), nadie juega dos veces en una jornada,
/// los descansos se reparten (uno por equipo si son impares) y se alterna local/visitante.
/// El orden inicial se sortea con una semilla, así la vista previa y lo que se guarda coinciden.
/// </summary>
public static class GeneradorCalendario
{
    private const int Descanso = 0;

    public static List<PartidoPropuesto> GenerarLiga(IReadOnlyList<int> equipos, ParametrosCalendario parametros)
    {
        if (equipos.Count < 2) throw new ArgumentException("Se necesitan al menos dos equipos.", nameof(equipos));
        if (parametros.Lugares.Count == 0) throw new ArgumentException("Se necesita al menos un lugar.", nameof(parametros));

        var lista = Sortear(equipos, parametros.Semilla);
        // Con equipos impares el "descanso" ocupa la posición fija del círculo: así cada equipo
        // descansa una vez y la localía queda equilibrada.
        if (lista.Count % 2 == 1) lista.Insert(0, Descanso);

        int n = lista.Count;
        int jornadasIda = n - 1;
        var emparejamientos = new List<(int Jornada, int Local, int Visitante)>();

        for (int ronda = 0; ronda < jornadasIda; ronda++)
        {
            for (int i = 0; i < n / 2; i++)
            {
                int a = lista[i];
                int b = lista[n - 1 - i];
                if (a == Descanso || b == Descanso) continue;

                // Alternancia de localía de la tabla de Berger: el equipo fijo alterna cada jornada
                // y el resto alterna según su posición.
                bool aEsLocal = i == 0 ? ronda % 2 == 0 : i % 2 == 1;
                emparejamientos.Add(aEsLocal ? (ronda + 1, a, b) : (ronda + 1, b, a));
            }

            // Rotación: el primero queda fijo y los demás giran una posición.
            int ultimo = lista[n - 1];
            lista.RemoveAt(n - 1);
            lista.Insert(1, ultimo);
        }

        if (parametros.IdaYVuelta)
        {
            emparejamientos.AddRange(emparejamientos.ToList()
                .Select(e => (e.Jornada + jornadasIda, e.Visitante, e.Local)));
        }

        return AsignarFechasYLugares(emparejamientos, parametros);
    }

    /// <summary>Fecha de la jornada, y dentro de la jornada reparte canchas y horarios escalonados.</summary>
    public static List<PartidoPropuesto> AsignarFechasYLugares(
        IEnumerable<(int Jornada, int Local, int Visitante)> emparejamientos, ParametrosCalendario parametros)
    {
        var resultado = new List<PartidoPropuesto>();
        foreach (var jornada in emparejamientos.GroupBy(e => e.Jornada).OrderBy(g => g.Key))
        {
            var dia = parametros.PrimeraFecha.AddDays((jornada.Key - 1) * parametros.DiasEntreJornadas);
            int espacio = 0;
            foreach (var (_, local, visitante) in jornada)
            {
                int lugar = parametros.Lugares[espacio % parametros.Lugares.Count];
                int turno = espacio / parametros.Lugares.Count;
                var fecha = dia.ToDateTime(parametros.Hora).AddMinutes(turno * parametros.MinutosEntrePartidos);
                resultado.Add(new PartidoPropuesto(jornada.Key, fecha, local, visitante, lugar));
                espacio++;
            }
        }

        return resultado;
    }

    public static List<int> Sortear(IEnumerable<int> elementos, int semilla)
    {
        var lista = elementos.ToList();
        var azar = new Random(semilla);
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = azar.Next(i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }

        return lista;
    }
}
