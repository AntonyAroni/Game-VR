using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures.Commands
{
    /// <summary>
    /// Conmutación de la linterna clínica entre luz blanca y lámpara de Wood UV (395 nm).
    /// Principio de Responsabilidad Única (SRP): sólo delega en la herramienta de iluminación.
    /// Metáfora IHC: la pinza índice-pulgar imita accionar el interruptor deslizante del mango.
    /// </summary>
    public class ToggleUvLightGestureCommand : IGestureCommand
    {
        public GestureCommandId Id => GestureCommandId.ToggleUvFlashlight;

        public string DisplayName => "Alternar luz UV";

        public bool CanExecute(GestureCommandContext context) => context != null && context.Flashlight != null;

        public bool Execute(GestureCommandContext context, HandSide hand)
        {
            var flashlight = context?.Flashlight;
            if (flashlight == null) return false;

            flashlight.ToggleLightMode();
            return true;
        }
    }
}
