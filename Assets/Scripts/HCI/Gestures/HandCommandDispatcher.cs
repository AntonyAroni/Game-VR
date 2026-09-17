using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.HCI.Gestures.Commands;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Traduce gestos confirmados en órdenes clínicas ejecutables y emite la respuesta multimodal.
    /// Principio de Responsabilidad Única (SRP): sólo enlaza gesto con orden y confirma al usuario.
    /// Principio de Inversión de Dependencias (DIP): trabaja contra <see cref="IGestureCommand"/>,
    /// nunca contra los controladores concretos del civil, del torso o de la linterna.
    ///
    /// Justificación IHC (Feedback): toda orden aceptada se confirma con vibración háptica en la
    /// mano que la emitió, sonido diegético del puesto y etiqueta legible en el HUD de manos.
    /// </summary>
    [DisallowMultipleComponent]
    public class HandCommandDispatcher : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private HandGestureRecognizer recognizer;

        [Header("Enlaces Gesto → Orden")]
        [SerializeField] private List<GestureCommandBinding> bindings = new List<GestureCommandBinding>();

        [Header("Retroalimentación Multimodal")]
        [SerializeField] private bool hapticFeedback = true;
        [SerializeField] private float hapticAmplitude = 0.6f;
        [SerializeField] private float hapticDuration = 0.09f;
        [SerializeField] private string acceptedSoundId = "button_click";
        [SerializeField] private string rejectedSoundId = "verdict_error";

        /// <summary>Orden resuelta: (mano emisora, etiqueta legible, fue ejecutada realmente).</summary>
        public event Action<HandSide, string, bool> CommandExecuted;

        /// <summary>Reconocedor al que está enganchado el despachador (lo consulta el HUD).</summary>
        public HandGestureRecognizer Recognizer => recognizer;

        /// <summary>Enlaces activos, expuestos para que el HUD dibuje la chuleta de gestos.</summary>
        public IReadOnlyList<GestureCommandBinding> Bindings => bindings;

        private readonly Dictionary<GestureCommandId, IGestureCommand> registry = new Dictionary<GestureCommandId, IGestureCommand>();
        private readonly GestureCommandContext context = new GestureCommandContext();

        private void Reset()
        {
            RestoreDefaultBindings();
        }

        /// <summary>
        /// Repuebla los enlaces gesto-orden con la configuración por defecto.
        /// Se invoca desde el instalador del Editor para que la escena los guarde serializados.
        /// </summary>
        public void RestoreDefaultBindings()
        {
            bindings = HandGestureCatalog.CreateDefaultBindings();
        }

        private void Awake()
        {
            if (bindings == null || bindings.Count == 0)
            {
                RestoreDefaultBindings();
            }

            RegisterCommands();

            if (recognizer == null && !TryGetComponent(out recognizer))
            {
                recognizer = FindAnyObjectByType<HandGestureRecognizer>();
            }

            if (recognizer == null)
            {
                Debug.LogWarning("[HandCommandDispatcher] No se encontró un HandGestureRecognizer: el módulo de comandos por manos quedará inactivo.");
            }
        }

        /// <summary>
        /// Registro de órdenes disponibles. Añadir una orden nueva sólo requiere
        /// implementar <see cref="IGestureCommand"/> y darla de alta aquí (OCP).
        /// </summary>
        private void RegisterCommands()
        {
            registry.Clear();
            Register(new ArmPostureGestureCommand(GestureCommandId.RaiseArms));
            Register(new ArmPostureGestureCommand(GestureCommandId.LowerArms));
            Register(new ArmPostureGestureCommand(GestureCommandId.ToggleRaiseArms));
            Register(new ExposeTorsoGestureCommand());
            Register(new HoldCivilianGestureCommand());
            Register(new ToggleUvLightGestureCommand());
            Register(new SubmitVerdictGestureCommand(VerdictType.ApprovedSafeZone));
            Register(new SubmitVerdictGestureCommand(VerdictType.SendToQuarantine));
        }

        private void Register(IGestureCommand command)
        {
            if (command == null || command.Id == GestureCommandId.None) return;
            registry[command.Id] = command;
        }

        private void OnEnable()
        {
            if (recognizer != null) recognizer.GesturePerformed += HandleGesturePerformed;
            EventBus.OnSurvivorArrived += HandleSurvivorArrived;
            EventBus.OnSurvivorDeparted += HandleSurvivorDeparted;
        }

        private void OnDisable()
        {
            if (recognizer != null) recognizer.GesturePerformed -= HandleGesturePerformed;
            EventBus.OnSurvivorArrived -= HandleSurvivorArrived;
            EventBus.OnSurvivorDeparted -= HandleSurvivorDeparted;
        }

        private void HandleSurvivorArrived(object survivor) => context.Invalidate();
        private void HandleSurvivorDeparted() => context.Invalidate();

        private void HandleGesturePerformed(HandSide hand, HandGestureType gesture, Vector3 palmPosition)
        {
            IGestureCommand command = ResolveCommand(gesture, hand);
            if (command == null) return;

            bool executed = command.CanExecute(context) && command.Execute(context, hand);

            if (executed)
            {
                if (hapticFeedback) EventBus.RequestHapticImpulse(hand, hapticAmplitude, hapticDuration);
                if (!string.IsNullOrEmpty(acceptedSoundId)) EventBus.RequestSpatialAudio(acceptedSoundId, palmPosition, 0.85f);
            }
            else if (!string.IsNullOrEmpty(rejectedSoundId))
            {
                // Feedback de rechazo: la orden se entendió pero no era aplicable en este estado.
                EventBus.RequestSpatialAudio(rejectedSoundId, palmPosition, 0.35f);
            }

            CommandExecuted?.Invoke(hand, command.DisplayName, executed);
            EventBus.TriggerHandCommandExecuted(command.DisplayName, executed);
        }

        private IGestureCommand ResolveCommand(HandGestureType gesture, HandSide hand)
        {
            for (int i = 0; i < bindings.Count; ++i)
            {
                GestureCommandBinding binding = bindings[i];
                if (binding == null || !binding.Handles(gesture, hand)) continue;
                if (registry.TryGetValue(binding.Command, out IGestureCommand command)) return command;
            }
            return null;
        }
    }
}
