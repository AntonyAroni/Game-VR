using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures.Commands
{
    /// <summary>
    /// Orden de descubrir o cubrir el torso del civil para inspección dermatológica.
    /// Principio de Responsabilidad Única (SRP): sólo conmuta la vestimenta superior.
    /// Metáfora IHC: señalar con el índice al pecho del sujeto equivale a "descúbrase ahí".
    /// </summary>
    public class ExposeTorsoGestureCommand : IGestureCommand
    {
        public GestureCommandId Id => GestureCommandId.ToggleExposeTorso;

        public string DisplayName => "Descubrir torso";

        public bool CanExecute(GestureCommandContext context) => context != null && context.Torso != null;

        public bool Execute(GestureCommandContext context, HandSide hand)
        {
            var torso = context?.Torso;
            if (torso == null) return false;

            torso.ToggleTorso();
            return true;
        }
    }
}
