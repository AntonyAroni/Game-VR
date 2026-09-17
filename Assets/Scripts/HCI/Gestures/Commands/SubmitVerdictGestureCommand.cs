using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures.Commands
{
    /// <summary>
    /// Emisión del veredicto final mediante gesto de pulgar (arriba = paso, abajo = cuarentena).
    /// Principio de Responsabilidad Única (SRP): sólo publica el veredicto en el <see cref="EventBus"/>;
    /// la validación de estado y la protección contra doble veredicto siguen residiendo
    /// en <c>CheckpointFlowManager</c>, que ignora veredictos fuera de la fase de inspección.
    /// Restricción IHC: el reconocedor exige un dwell prolongado por ser una acción irreversible.
    /// </summary>
    public class SubmitVerdictGestureCommand : IGestureCommand
    {
        private readonly VerdictType verdict;

        public SubmitVerdictGestureCommand(VerdictType verdictType)
        {
            verdict = verdictType;
        }

        public GestureCommandId Id => verdict == VerdictType.ApprovedSafeZone
            ? GestureCommandId.ApproveSafeZone
            : GestureCommandId.SendToQuarantine;

        public string DisplayName => verdict == VerdictType.ApprovedSafeZone
            ? "APROBADO · Zona segura"
            : "CUARENTENA";

        public bool CanExecute(GestureCommandContext context) => verdict != VerdictType.None;

        public bool Execute(GestureCommandContext context, HandSide hand)
        {
            if (verdict == VerdictType.None) return false;

            EventBus.TriggerVerdictSubmitted(verdict);
            return true;
        }
    }
}
