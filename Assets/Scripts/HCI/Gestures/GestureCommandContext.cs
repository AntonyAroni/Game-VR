using UnityEngine;
using ZombieCheckpoint.Survivors;
using ZombieCheckpoint.Tools;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Resolución perezosa y segura de las referencias de escena que necesitan las órdenes.
    /// Principio de Responsabilidad Única (SRP): sólo localiza y cachea actores vivos.
    /// Los civiles se destruyen e instancian en cada ronda, por lo que la caché se
    /// revalida con comparación contra <c>null</c> de Unity (protección frente a "fake-null").
    /// Nunca se invoca desde <c>Update()</c>: sólo al confirmarse un gesto.
    /// </summary>
    public class GestureCommandContext
    {
        private SurvivorHumanoidController humanoid;
        private TorsoClothingController torso;
        private FlashlightTool flashlight;

        /// <summary>Controlador cinemático del civil actualmente en el puesto.</summary>
        public SurvivorHumanoidController Humanoid
        {
            get
            {
                if (humanoid == null)
                {
                    humanoid = Object.FindAnyObjectByType<SurvivorHumanoidController>();
                }
                return humanoid;
            }
        }

        /// <summary>Controlador de vestimenta / torso descubierto del civil actual.</summary>
        public TorsoClothingController Torso
        {
            get
            {
                if (torso == null)
                {
                    torso = Object.FindAnyObjectByType<TorsoClothingController>();
                }
                return torso;
            }
        }

        /// <summary>Linterna clínica presente en el mostrador.</summary>
        public FlashlightTool Flashlight
        {
            get
            {
                if (flashlight == null)
                {
                    flashlight = Object.FindAnyObjectByType<FlashlightTool>();
                }
                return flashlight;
            }
        }

        /// <summary>
        /// Invalida la caché al cambiar de ronda para no arrastrar referencias
        /// a civiles ya despachados y destruidos.
        /// </summary>
        public void Invalidate()
        {
            humanoid = null;
            torso = null;
            flashlight = null;
        }
    }
}
