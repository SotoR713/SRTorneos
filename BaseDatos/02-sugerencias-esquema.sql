/*
    SRTorneos - SUGERENCIAS para el esquema de SRTorneosBD.
    NO es necesario ejecutar nada de esto para que la aplicación funcione.
    Cada bloque es independiente: revisa, ajusta y ejecuta solo lo que decidas.
    Antes de crear un índice UNIQUE asegúrate de que no haya duplicados.
*/
USE SRTorneosBD;
GO

/* ---------------------------------------------------------------------------
   1. SEGURIDAD: Usuarios.Contraseña guarda texto plano (varchar 50).
      Nunca guardes la contraseña tal cual: guarda un hash con sal (PBKDF2,
      p. ej. con Rfc2898DeriveBytes.Pbkdf2 en .NET). Cuando agreguemos el
      inicio de sesión se usarían columnas así:
--------------------------------------------------------------------------- */
-- ALTER TABLE Usuarios ADD ContrasenaHash VARBINARY(64) NULL, ContrasenaSal VARBINARY(16) NULL;
-- (después de migrar)  ALTER TABLE Usuarios DROP COLUMN Contraseña;

/* ---------------------------------------------------------------------------
   2. Evitar duplicados que la app ya valida, pero que la base debería garantizar.
--------------------------------------------------------------------------- */
-- Un equipo se inscribe una sola vez por torneo
CREATE UNIQUE INDEX UX_Inscripciones_Torneo_Equipo ON Inscripciones (IdTorneo, IdEquipo);
-- Un jugador juega para un solo equipo dentro de un torneo
CREATE UNIQUE INDEX UX_Alineaciones_Torneo_Jugador ON Alineaciones (IdTorneo, IdJugador);
-- Un marcador por equipo y partido
CREATE UNIQUE INDEX UX_Puntajes_Partido_Equipo ON Puntajes (IdPartido, IdEquipo);
-- Una configuración por torneo
CREATE UNIQUE INDEX UX_ConfigTorn_Torneo ON ConfigTorn (IdTorneo);
-- Nombres de equipo únicos
CREATE UNIQUE INDEX UX_Equipos_Nombre ON Equipos (Nombre);
GO

/* ---------------------------------------------------------------------------
   3. Reglas de negocio con CHECK.
--------------------------------------------------------------------------- */
ALTER TABLE Torneos   ADD CONSTRAINT CK_Torneos_Fechas     CHECK (FechaFin >= FechaInicio);
ALTER TABLE Partidos  ADD CONSTRAINT CK_Partidos_Equipos   CHECK (IdEquipo1 <> IdEquipo2);
ALTER TABLE Puntajes  ADD CONSTRAINT CK_Puntajes_Puntos    CHECK (Puntos >= 0);
ALTER TABLE Anotacion ADD CONSTRAINT CK_Anotacion_Puntos   CHECK (Puntos >= 0);
ALTER TABLE ConfigTorn ADD CONSTRAINT CK_ConfigTorn_Valores CHECK (PuntosVict > 0 AND EquiposMax >= 2);
GO

/* ---------------------------------------------------------------------------
   4. Índices para las llaves foráneas más consultadas (SQL Server no los crea solo).
--------------------------------------------------------------------------- */
CREATE INDEX IX_Partidos_Torneo        ON Partidos (IdTorneo, Fecha);
CREATE INDEX IX_Alineaciones_Equipo    ON Alineaciones (IdTorneo, IdEquipo);
CREATE INDEX IX_Anotacion_Partido      ON Anotacion (IdPartido) INCLUDE (IdJugador, Puntos);
CREATE INDEX IX_Faltas_Partido         ON Faltas (IdPartido);
CREATE INDEX IX_Faltas_Torneo_Jugador  ON Faltas (IdTorneo, IdJugador);
GO

/* ---------------------------------------------------------------------------
   5. Diseño (para pensar con calma; implican cambios en la app):
   a) Inscripciones.IdPuesto es NOT NULL, pero el puesto final no se conoce al
      inscribir. Hazlo NULL o usa el Puesto 0 "Sin definir" (así lo maneja hoy la app).
         ALTER TABLE Inscripciones ALTER COLUMN IdPuesto INT NULL;
   b) ConfigTorn.IdTipoTorneo repite Torneos.IdTipoTorneo; conviene dejarlo en un
      solo lugar (la app hoy mantiene ambos iguales).
   c) Faltas.IdTorneo se puede obtener de Partidos.IdTorneo (dato repetido).
   d) Jugadores.Numero es global; en la práctica el número depende del equipo y
      torneo. Considera moverlo a Alineaciones (Alineaciones.Numero).
   e) Multideporte: ConfigTorn podría tener PuntosEmpate y PuntosDerrota
      (FIBA da 1 punto por derrota; el fútbol 1 por empate). Hoy solo hay PuntosVict.
   f) Partidos podría tener IdEstado (Programado, Jugado, Suspendido) en lugar de
      deducir "jugado" por la existencia de Puntajes.
--------------------------------------------------------------------------- */
