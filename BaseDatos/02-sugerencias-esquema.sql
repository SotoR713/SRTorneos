/*
    SRTorneos - SQL de las mejoras sugeridas para SRTorneosBD (opcionales).
    La explicación y tus decisiones están en la guía «SRTorneos — Guía de mejoras y correcciones».
    La aplicación funciona con la base tal como está; nada de esto es obligatorio.
    Todo va comentado: quita los dos guiones solo de lo que decidas aplicar, revísalo y ejecútalo tú en SSMS.

    Esquema que usa la aplicación (2026-10-20):
      Torneos(Id, Nombre, FechaInicio, FechaFin, IdDeporte, IdUsuario) · ConfigTorn(Id, IdTorneo, PuntosVict, EquiposMax, IdTipoTorneo)
      Equipos(Id, Nombre, Clave) · Jugadores(Id, Numero, Nombre, Posicion) · Lugares(Id, Nombre, Direccion)
      Inscripciones(Id, IdTorneo, IdEquipo, IdEstado, IdPuesto) · Alineaciones(Id, IdTorneo, IdEquipo, IdJugador)
      Partidos(Id, Fecha, IdEquipo1, IdEquipo2, IdLugar, IdTorneo) · Puntajes(Id, IdPartido, IdEquipo, Puntos)
      Anotacion(Id, IdJugador, IdPartido, Puntos) · Faltas(Id, IdJugador, IdPartido, IdTipoFalta)  -- el torneo sale de Partidos
      Catálogos (Id, Nombre): Deportes, TiposTorneos, Estados, Puestos (texto), TiposFaltas · Usuarios(Id, UserName, Contraseña)
*/
USE SRTorneosBD;
GO

/* Guía #1 — Anotaciones de cualquier valor (tiros libres, goles…).
   Después avisa para cambiar "Anotaciones:ValoresPermitidos" en appsettings.json. */
-- ALTER TABLE Anotacion DROP CONSTRAINT CK_Anotacion_Puntos;
-- ALTER TABLE Anotacion ADD CONSTRAINT CK_Anotacion_Puntos CHECK (Puntos > 0);

/* Guía #4 — La clave del equipo no se repite (el nombre sí puede repetirse). La página ya lo valida. */
-- CREATE UNIQUE INDEX UX_Equipos_Clave ON Equipos (Clave);

/* Guía #7 — Fechas en orden y marcadores no negativos. La página ya lo valida. */
-- ALTER TABLE Torneos  ADD CONSTRAINT CK_Torneos_Fechas  CHECK (FechaFin >= FechaInicio);
-- ALTER TABLE Puntajes ADD CONSTRAINT CK_Puntajes_Puntos CHECK (Puntos >= 0);

/* Guía #8 — Índices: SQL Server encuentra los datos sin recorrer toda la tabla (solo rapidez). */
-- CREATE INDEX IX_Partidos_Torneo        ON Partidos (IdTorneo, Fecha);
-- CREATE INDEX IX_Anotacion_Partido      ON Anotacion (IdPartido) INCLUDE (IdJugador, Puntos);
-- CREATE INDEX IX_Faltas_Partido         ON Faltas (IdPartido);
-- CREATE INDEX IX_Alineaciones_Equipo    ON Alineaciones (IdTorneo, IdEquipo);

/* Para más adelante (guía #3, #5, #6, #9, #10, #14): asistencia por partido, jugador único por torneo,
   rondas y llaves, estado del partido, puntos por empate/derrota y logo/color del equipo.
   Cuando decidas alguno, se prepara su SQL y se adapta la página. */
