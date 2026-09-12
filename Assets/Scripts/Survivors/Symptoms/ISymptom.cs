namespace ZombieCheckpoint.Survivors.Symptoms
{
    /// <summary>
    /// Interfaz que define un síntoma biológico inspeccionable en el superviviente.
    /// Principio Open/Closed (OCP): Permite agregar nuevos síntomas (fiebre, erupciones, etc.) sin modificar el evaluador.
    /// </summary>
    public interface ISymptom
    {
        string SymptomName { get; }
        
        /// <summary>
        /// Indica si este síntoma específico denota infección zombi activa.
        /// </summary>
        bool IsPositiveForInfection { get; }
        
        /// <summary>
        /// Indica si el jugador ya descubrió e interactuó exitosamente con este síntoma.
        /// </summary>
        bool IsDiscovered { get; }

        /// <summary>
        /// Ejecuta la lógica de examen al aplicar una herramienta o inspección visual directa.
        /// </summary>
        bool TryExamine(string toolName, out string feedbackMessage);
    }
}
