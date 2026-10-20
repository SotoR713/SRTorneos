/*
    SRTorneos - SUGERENCIAS para el esquema de SRTorneosBD (revisado contra tus
    restricciones e índices reales del 2026-10-08).
    NO es necesario ejecutar nada de esto para que la aplicación funcione.
    Cada bloque es independiente: revisa, ajusta y ejecuta solo lo que decidas.

    Ya tienes (bien): UQ en Inscripciones(IdTorneo, IdEquipo), Puntajes(IdPartido, IdEquipo),
    ConfigTorn(IdTorneo), nombres de catálogos y Usuarios.UserName; CK_Partidos_EquiposDiferentes.
*/
USE SRTorneosBD;
GO

/* ---------------------------------------------------------------------------
   1. SEGURIDAD: Usuarios.Contraseña guarda texto plano (varchar 50).
      Guarda un hash con sal (PBKDF2). Se usará cuando agreguemos el inicio de sesión.
--------------------------------------------------------------------------- */
-- ALTER TABLE Usuarios ADD ContrasenaHash VARBINARY(64) NULL, ContrasenaSal VARBINARY(16) NULL;
-- (después de migrar)  ALTER TABLE Usuarios DROP COLUMN Contraseña;

/* ---------------------------------------------------------------------------
   2. ANOTACIONES: CK_Anotacion_Puntos solo acepta 2 o 3, así que los tiros libres
      (1 punto) no se pueden registrar por jugador, y en otros deportes (un gol = 1)
      tampoco se podría. Opciones:
        a) Solo básquetbol completo:   CHECK (Puntos IN (1, 2, 3))
        b) Multideporte (recomendado): CHECK (Puntos > 0)
      IMPORTANTE: si cambias la regla, actualiza en appsettings.json
      "Anotaciones:ValoresPermitidos" (p. ej. [ 1, 2, 3 ]) para que la app muestre la columna.
--------------------------------------------------------------------------- */
-- ALTER TABLE Anotacion DROP CONSTRAINT CK_Anotacion_Puntos;
-- ALTER TABLE Anotacion ADD CONSTRAINT CK_Anotacion_Puntos CHECK (Puntos IN (1, 2, 3));

/* ---------------------------------------------------------------------------
   3. NÚMERO DE JUGADOR: YA APLICADO (se quitó UQ_Jugadores_Numero; la app valida que el
      número no se repita dentro del mismo equipo). Opcional a futuro: moverlo a Alineaciones:
--------------------------------------------------------------------------- */
-- ALTER TABLE Alineaciones ADD Numero INT NULL;
-- CREATE UNIQUE INDEX UX_Alineaciones_Numero ON Alineaciones (IdTorneo, IdEquipo, Numero) WHERE Numero IS NOT NULL;
-- (avísame si lo haces para cambiar la validación de la app a "único por equipo")

/* ---------------------------------------------------------------------------
   4. ALINEACIONES: UQ_Alineaciones (IdTorneo, IdEquipo, IdJugador) permite que un mismo
      jugador esté en dos equipos del mismo torneo. La app ya lo impide, pero la base
      debería garantizarlo:
--------------------------------------------------------------------------- */
-- CREATE UNIQUE INDEX UX_Alineaciones_Torneo_Jugador ON Alineaciones (IdTorneo, IdJugador);

/* ---------------------------------------------------------------------------
   5. Otras reglas que faltan (la app ya las valida).
--------------------------------------------------------------------------- */
-- CREATE UNIQUE INDEX UX_Equipos_Nombre ON Equipos (Nombre);
-- ALTER TABLE Torneos    ADD CONSTRAINT CK_Torneos_Fechas      CHECK (FechaFin >= FechaInicio);
-- ALTER TABLE Puntajes   ADD CONSTRAINT CK_Puntajes_Puntos     CHECK (Puntos >= 0);
-- ALTER TABLE ConfigTorn ADD CONSTRAINT CK_ConfigTorn_Valores  CHECK (PuntosVict > 0 AND EquiposMax >= 2);

/* ---------------------------------------------------------------------------
   6. Índices para las llaves foráneas más consultadas (SQL Server no los crea solo).
--------------------------------------------------------------------------- */
-- CREATE INDEX IX_Partidos_Torneo        ON Partidos (IdTorneo, Fecha);
-- CREATE INDEX IX_Anotacion_Partido      ON Anotacion (IdPartido) INCLUDE (IdJugador, Puntos);
-- CREATE INDEX IX_Faltas_Partido         ON Faltas (IdPartido);
-- CREATE INDEX IX_Faltas_Torneo_Jugador  ON Faltas (IdTorneo, IdJugador);

/* ---------------------------------------------------------------------------
   7. Diseño (para pensar con calma; implican cambios en la app):
   a) Inscripciones.IdPuesto es NOT NULL, pero el puesto final no se conoce al inscribir.
      Hoy se usa el Puesto 0 "Sin definir". Alternativa: ALTER COLUMN IdPuesto INT NULL.
   b) El tipo de torneo vive solo en ConfigTorn.IdTipoTorneo (la app ya lo lee de ahí).
   c) Faltas.IdTorneo se puede obtener de Partidos.IdTorneo (dato repetido).
   d) Multideporte: ConfigTorn podría tener PuntosEmpate y PuntosDerrota.
   e) Partidos podría tener IdEstado (Programado, Jugado, Suspendido).
--------------------------------------------------------------------------- */
