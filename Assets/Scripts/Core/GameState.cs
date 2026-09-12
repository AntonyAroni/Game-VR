namespace ZombieCheckpoint.Core
{
    /// <summary>
    /// Representa los diferentes estados en el ciclo de inspección de un superviviente.
    /// Principio de Responsabilidad Única (SRP): Define únicamente la máquina de estados del puesto.
    /// </summary>
    public enum InspectionState
    {
        /// <summary>Esperando que el siguiente superviviente se posicione en la cabina.</summary>
        WaitingNextSurvivor,
        
        /// <summary>Superviviente en posición; el jugador puede examinar cuerpo, herramientas y documentos.</summary>
        Inspecting,
        
        /// <summary>El jugador ha tomado o iniciado una acción de veredicto (sello o palanca).</summary>
        AwaitingDecision,
        
        /// <summary>Evaluando resultado, retroalimentación al jugador y transición de salida.</summary>
        EvaluatingResult,

        /// <summary>Jornada concluida o resumen de métricas IHC.</summary>
        SessionCompleted
    }

    /// <summary>
    /// Tipo de decisión tomada por el evaluador en el puesto de control.
    /// </summary>
    public enum VerdictType
    {
        None,
        ApprovedSafeZone,
        SendToQuarantine
    }
}
