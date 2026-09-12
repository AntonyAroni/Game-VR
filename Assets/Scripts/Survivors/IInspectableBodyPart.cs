using UnityEngine;

namespace ZombieCheckpoint.Survivors
{
    /// <summary>
    /// Interfaz para cualquier zona corporal o extremidad que responda a interacciones VR (agarre, rotación, herramientas).
    /// </summary>
    public interface IInspectableBodyPart
    {
        string BodyPartName { get; }
        Transform PartTransform { get; }
        
        /// <summary>
        /// Aplica una herramienta a esta parte del cuerpo. Retorna true si hubo una reacción.
        /// </summary>
        bool ReceiveInspection(string toolName);

        /// <summary>
        /// Resalta visualmente la parte del cuerpo como indicación de aforismo (affordance de agarre/interacción).
        /// </summary>
        void SetHighlight(bool highlighted);
    }
}
