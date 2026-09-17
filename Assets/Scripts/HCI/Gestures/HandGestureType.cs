namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Vocabulario de gestos manuales reconocibles por el módulo de comandos por manos.
    /// Principio de Responsabilidad Única (SRP): define únicamente el léxico gestual,
    /// sin conocer qué acción clínica dispara cada gesto (eso lo resuelve el despachador).
    /// </summary>
    public enum HandGestureType
    {
        /// <summary>Ningún gesto reconocido (mano en reposo o postura ambigua).</summary>
        None,

        /// <summary>Mano abierta con la palma hacia el cielo: metáfora de "arriba" / "levante".</summary>
        OpenPalmUp,

        /// <summary>Mano abierta con la palma hacia el suelo: metáfora de "abajo" / "baje".</summary>
        OpenPalmDown,

        /// <summary>Mano abierta con la palma hacia el sujeto: metáfora universal de "alto".</summary>
        OpenPalmForward,

        /// <summary>Puño cerrado: metáfora de "retener" / "cancelar".</summary>
        Fist,

        /// <summary>Pulgar extendido hacia arriba: aprobación.</summary>
        ThumbUp,

        /// <summary>Pulgar extendido hacia abajo: rechazo / cuarentena.</summary>
        ThumbDown,

        /// <summary>Índice extendido señalando al frente: metáfora de "señalar esa zona".</summary>
        PointIndex,

        /// <summary>Pinza índice-pulgar: metáfora de "tomar/alternar" un conmutador fino.</summary>
        PinchIndex
    }

    /// <summary>
    /// Restricción de orientación espacial exigida a un gesto para ser válido.
    /// Evita falsos positivos: la forma de los dedos por sí sola no basta,
    /// la mano debe además estar orientada de forma coherente con la metáfora.
    /// </summary>
    public enum GestureOrientation
    {
        /// <summary>Sin restricción de orientación.</summary>
        Ignore,

        /// <summary>La palma mira hacia el cielo.</summary>
        PalmUp,

        /// <summary>La palma mira hacia el suelo.</summary>
        PalmDown,

        /// <summary>La palma mira en la dirección de la mirada del jugador (hacia el civil).</summary>
        PalmAwayFromFace,

        /// <summary>La palma mira hacia la cara del jugador.</summary>
        PalmTowardsFace,

        /// <summary>El pulgar apunta al cielo.</summary>
        ThumbUp,

        /// <summary>El pulgar apunta al suelo.</summary>
        ThumbDown,

        /// <summary>El índice apunta en la dirección de la mirada del jugador.</summary>
        IndexForward
    }
}
