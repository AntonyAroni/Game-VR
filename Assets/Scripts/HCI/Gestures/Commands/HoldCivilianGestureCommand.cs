using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures.Commands
{
    /// <summary>
    /// Orden de detención inmediata del civil ("¡Alto!").
    /// Principio de Responsabilidad Única (SRP): sólo interrumpe la locomoción del PNJ.
    /// Metáfora IHC: palma abierta al frente, el gesto universal de detención usado en controles.
    /// </summary>
    public class HoldCivilianGestureCommand : IGestureCommand
    {
        public GestureCommandId Id => GestureCommandId.HoldCivilian;

        public string DisplayName => "¡Alto!";

        public bool CanExecute(GestureCommandContext context) => context != null && context.Humanoid != null;

        public bool Execute(GestureCommandContext context, HandSide hand)
        {
            var humanoid = context?.Humanoid;
            if (humanoid == null) return false;
            if (!humanoid.IsWalking) return false;

            humanoid.SetWalking(false);
            return true;
        }
    }
}
