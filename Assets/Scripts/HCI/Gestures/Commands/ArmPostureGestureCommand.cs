using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures.Commands
{
    /// <summary>
    /// Orden de postura de brazos del civil ("levante los brazos" / "bájelos").
    /// Principio de Responsabilidad Única (SRP): sólo altera la pose de inspección torácica.
    /// Metáfora IHC: la palma hacia arriba eleva, la palma hacia abajo baja (mapeo natural directo).
    /// </summary>
    public class ArmPostureGestureCommand : IGestureCommand
    {
        private readonly GestureCommandId id;

        public ArmPostureGestureCommand(GestureCommandId commandId)
        {
            id = commandId;
        }

        public GestureCommandId Id => id;

        public string DisplayName => id switch
        {
            GestureCommandId.RaiseArms => "¡Levante los brazos!",
            GestureCommandId.LowerArms => "Baje los brazos",
            _ => "Alternar brazos"
        };

        public bool CanExecute(GestureCommandContext context) => context != null && context.Humanoid != null;

        public bool Execute(GestureCommandContext context, HandSide hand)
        {
            var humanoid = context?.Humanoid;
            if (humanoid == null) return false;

            switch (id)
            {
                case GestureCommandId.RaiseArms:
                    if (humanoid.IsInspectionPose) return false;
                    humanoid.SetInspectionPose(true);
                    return true;

                case GestureCommandId.LowerArms:
                    if (!humanoid.IsInspectionPose) return false;
                    humanoid.SetInspectionPose(false);
                    return true;

                default:
                    humanoid.ToggleInspectionPose();
                    return true;
            }
        }
    }
}
