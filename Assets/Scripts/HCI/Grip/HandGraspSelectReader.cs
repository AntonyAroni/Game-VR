using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace ZombieCheckpoint.HCI.Grip
{
    /// <summary>
    /// Lector de selección que amplía la pinza original con el cierre completo de la mano.
    /// Principio Abierto/Cerrado (OCP): se instala como <c>bypass</c> del lector original de XRI,
    /// así que la pinza sigue funcionando exactamente igual y el puño se suma como segunda vía.
    ///
    /// Justificación IHC (Mapeo Natural): en la vida real una linterna o un sello se cogen
    /// cerrando la mano alrededor del mango, no pellizcándolos con dos dedos.
    /// </summary>
    public class HandGraspSelectReader : IXRInputButtonReader
    {
        private readonly XRInputButtonReader original;
        private readonly PalmGripAnchor anchor;

        private int evaluatedFrame = -1;
        private bool isPerformed;
        private bool wasPerformedThisFrame;
        private bool wasCompletedThisFrame;
        private float value;

        public HandGraspSelectReader(XRInputButtonReader originalReader, PalmGripAnchor owner)
        {
            original = originalReader;
            anchor = owner;
        }

        public bool ReadIsPerformed()
        {
            Evaluate();
            return isPerformed;
        }

        public bool ReadWasPerformedThisFrame()
        {
            Evaluate();
            return wasPerformedThisFrame;
        }

        public bool ReadWasCompletedThisFrame()
        {
            Evaluate();
            return wasCompletedThisFrame;
        }

        public float ReadValue()
        {
            Evaluate();
            return value;
        }

        public bool TryReadValue(out float readValue)
        {
            bool originalAvailable = original != null && original.TryReadValue(out _);
            Evaluate();
            readValue = value;
            return originalAvailable || (anchor != null && anchor.IsHandTracked);
        }

        /// <summary>
        /// Combina ambas vías una sola vez por fotograma. Los flancos se derivan del nivel
        /// combinado para que XRI vea una única pulsación aunque pinza y puño coincidan.
        /// Dentro de un bypass, las lecturas al lector original devuelven su valor crudo
        /// (XRI activa un ámbito que impide la recursión).
        /// </summary>
        private void Evaluate()
        {
            int frame = Time.frameCount;
            if (frame == evaluatedFrame) return;
            evaluatedFrame = frame;

            bool previous = isPerformed;

            bool pinchPerformed = original != null && original.ReadIsPerformed();
            float pinchValue = original != null ? original.ReadValue() : 0f;
            bool graspPerformed = anchor != null && anchor.IsGraspEngaged;

            isPerformed = pinchPerformed || graspPerformed;
            value = graspPerformed ? 1f : pinchValue;
            wasPerformedThisFrame = isPerformed && !previous;
            wasCompletedThisFrame = !isPerformed && previous;
        }
    }
}
