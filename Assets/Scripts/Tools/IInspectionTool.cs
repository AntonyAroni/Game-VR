using UnityEngine;

namespace ZombieCheckpoint.Tools
{
    /// <summary>
    /// Interfaz para todas las herramientas físicas de inspección en el puesto (estetoscopio, linterna, sello).
    /// Principio OCP: Permite incorporar nuevas herramientas sin alterar los detectores corporales.
    /// </summary>
    public interface IInspectionTool
    {
        string ToolName { get; }
        bool IsGrabbed { get; }
        
        /// <summary>Acción principal activada por el gatillo del mando (VR Trigger).</summary>
        void OnPrimaryActionTriggered();

        /// <summary>Aplica el efecto de la herramienta sobre un colisionador o parte corporal.</summary>
        void ApplyToTarget(GameObject target);
    }
}
