using System.Text;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI.Gestures
{
    /// <summary>
    /// Orquestador de la retroalimentación visual del módulo de comandos por manos.
    /// Principio de Responsabilidad Única (SRP): decide qué widget se muestra y cuándo,
    /// delegando el dibujado en <see cref="HandFeedbackRing"/> y <see cref="HandCheatSheetPanel"/>.
    ///
    /// Justificación IHC (Visibilidad sin invasión): no existe ningún panel fijo delante de la
    /// cara. El anillo de confirmación vive sobre la palma que gesticula y se desvanece al
    /// terminar; la chuleta de gestos sólo aparece cuando el usuario gira la palma hacia sí
    /// mismo, de modo que el campo visual queda libre para inspeccionar al civil.
    /// </summary>
    public class HandCommandHud : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private HandCommandDispatcher dispatcher;

        [Header("Anclaje Espacial")]
        [Tooltip("Cámara del visor. Si queda vacío se resuelve en tiempo de ejecución.")]
        [SerializeField] private Transform headAnchor;

        [Header("Chuleta de Gestos en la Palma")]
        [Tooltip("Muestra el listado de órdenes al girar la palma hacia la cara.")]
        [SerializeField] private bool enableCheatSheet = true;
        [Tooltip("Coseno mínimo entre la normal de la palma y la dirección a la cabeza.")]
        [Range(0.3f, 1f)]
        [SerializeField] private float palmFacingDot = 0.70f;
        [Tooltip("Coseno mínimo entre la dirección de los dedos y el eje vertical.")]
        [Range(0f, 1f)]
        [SerializeField] private float fingersUpDot = 0.35f;
        [Tooltip("Distancia máxima entre la cabeza y la palma para consultar la chuleta.")]
        [SerializeField] private float cheatSheetMaxDistance = 0.75f;
        [Tooltip("Tiempo que hay que sostener la palma antes de desplegar la chuleta.")]
        [SerializeField] private float cheatSheetHoldSeconds = 0.35f;

        private HandFeedbackRing leftRing;
        private HandFeedbackRing rightRing;
        private HandCheatSheetPanel cheatSheet;

        private HandGestureFeedback leftFeedback;
        private HandGestureFeedback rightFeedback;
        private float leftFacingSince = -1f;
        private float rightFacingSince = -1f;

        private readonly StringBuilder builder = new StringBuilder(256);
        private bool cheatSheetDirty = true;

        private void Awake()
        {
            if (dispatcher == null && !TryGetComponent(out dispatcher))
            {
                dispatcher = FindAnyObjectByType<HandCommandDispatcher>();
            }

            leftRing = CreateChild<HandFeedbackRing>("HandFeedback_Left");
            rightRing = CreateChild<HandFeedbackRing>("HandFeedback_Right");
            leftRing.Initialize(headAnchor);
            rightRing.Initialize(headAnchor);

            cheatSheet = CreateChild<HandCheatSheetPanel>("HandCheatSheet");
            cheatSheet.Initialize(headAnchor);
        }

        private void OnEnable()
        {
            if (dispatcher == null) return;

            dispatcher.CommandExecuted += HandleCommandExecuted;
            if (dispatcher.Recognizer != null)
            {
                dispatcher.Recognizer.GestureFeedbackUpdated += HandleGestureFeedback;
            }
        }

        private void OnDisable()
        {
            if (dispatcher == null) return;

            dispatcher.CommandExecuted -= HandleCommandExecuted;
            if (dispatcher.Recognizer != null)
            {
                dispatcher.Recognizer.GestureFeedbackUpdated -= HandleGestureFeedback;
            }
        }

        private void LateUpdate()
        {
            EnsureHeadAnchor();

            if (cheatSheetDirty)
            {
                RefreshCheatSheetContent();
                cheatSheetDirty = false;
            }

            UpdateCheatSheetVisibility();
        }

        private void EnsureHeadAnchor()
        {
            if (headAnchor != null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            headAnchor = cam.transform;
            leftRing.SetHeadAnchor(headAnchor);
            rightRing.SetHeadAnchor(headAnchor);
            cheatSheet.SetHeadAnchor(headAnchor);
        }

        private void HandleGestureFeedback(HandGestureFeedback feedback)
        {
            if (feedback.Side == HandSide.Left)
            {
                leftFeedback = feedback;
                leftRing.ApplyFeedback(feedback);
            }
            else
            {
                rightFeedback = feedback;
                rightRing.ApplyFeedback(feedback);
            }
        }

        private void HandleCommandExecuted(HandSide hand, string commandLabel, bool accepted)
        {
            HandFeedbackRing ring = hand == HandSide.Left ? leftRing : rightRing;
            ring.FlashConfirmation(commandLabel, accepted);
        }

        /// <summary>
        /// La chuleta se despliega al girar la palma hacia la cara con los dedos hacia arriba,
        /// el gesto natural de "consultar algo escrito en la mano". Exige sostenerlo un instante
        /// para que no parpadee mientras el usuario mueve las manos con normalidad.
        /// </summary>
        private void UpdateCheatSheetVisibility()
        {
            if (!enableCheatSheet || cheatSheet == null)
            {
                cheatSheet?.Hide();
                return;
            }

            float now = Time.unscaledTime;
            bool leftReady = EvaluateFacing(leftFeedback, ref leftFacingSince, now);
            bool rightReady = EvaluateFacing(rightFeedback, ref rightFacingSince, now);

            if (leftReady)
            {
                cheatSheet.ShowAt(leftFeedback.PalmPosition, leftFeedback.PalmNormal);
            }
            else if (rightReady)
            {
                cheatSheet.ShowAt(rightFeedback.PalmPosition, rightFeedback.PalmNormal);
            }
            else
            {
                cheatSheet.Hide();
            }
        }

        private bool EvaluateFacing(in HandGestureFeedback feedback, ref float facingSince, float now)
        {
            if (!IsPalmFacingUser(feedback))
            {
                facingSince = -1f;
                return false;
            }

            if (facingSince < 0f) facingSince = now;
            return now - facingSince >= cheatSheetHoldSeconds;
        }

        private bool IsPalmFacingUser(in HandGestureFeedback feedback)
        {
            if (!feedback.IsHandTracked || headAnchor == null) return false;

            // Leer el pasaporte en la mano usa la misma postura: con un objeto agarrado no se despliega.
            if (feedback.IsHoldingObject) return false;

            // Mientras se confirma una orden la mano está ocupada: la chuleta no debe estorbar.
            if (feedback.Progress > 0.02f) return false;

            Vector3 toHead = headAnchor.position - feedback.PalmPosition;
            if (toHead.sqrMagnitude > cheatSheetMaxDistance * cheatSheetMaxDistance) return false;
            if (toHead.sqrMagnitude < 1e-6f) return false;

            if (Vector3.Dot(feedback.PalmNormal, toHead.normalized) < palmFacingDot) return false;
            return Vector3.Dot(feedback.PalmForward, Vector3.up) >= fingersUpDot;
        }

        private void RefreshCheatSheetContent()
        {
            if (cheatSheet == null || dispatcher == null) return;

            builder.Clear();
            var bindings = dispatcher.Bindings;
            for (int i = 0; i < bindings.Count; ++i)
            {
                GestureCommandBinding binding = bindings[i];
                if (binding == null || !binding.IsEnabled) continue;

                string handTag = binding.Hand switch
                {
                    HandSide.Left => " (izq)",
                    HandSide.Right => " (der)",
                    _ => string.Empty
                };

                builder.Append("<b>").Append(ResolveGestureLabel(binding.Gesture)).Append(handTag)
                       .Append("</b>  →  ").Append(DescribeCommand(binding.Command)).Append('\n');
            }

            cheatSheet.SetContent(builder.ToString());
        }

        private string ResolveGestureLabel(HandGestureType gesture)
        {
            var recognizer = dispatcher != null ? dispatcher.Recognizer : null;
            if (recognizer == null) return gesture.ToString();

            var definitions = recognizer.Definitions;
            for (int i = 0; i < definitions.Count; ++i)
            {
                if (definitions[i] != null && definitions[i].Type == gesture) return definitions[i].DisplayName;
            }
            return gesture.ToString();
        }

        private static string DescribeCommand(GestureCommandId command) => command switch
        {
            GestureCommandId.RaiseArms => "Levantar brazos",
            GestureCommandId.LowerArms => "Bajar brazos",
            GestureCommandId.ToggleRaiseArms => "Alternar brazos",
            GestureCommandId.ToggleExposeTorso => "Descubrir torso",
            GestureCommandId.ApproveSafeZone => "APROBAR",
            GestureCommandId.SendToQuarantine => "CUARENTENA",
            GestureCommandId.HoldCivilian => "Ordenar alto",
            GestureCommandId.ToggleUvFlashlight => "Luz UV",
            _ => "—"
        };

        private T CreateChild<T>(string childName) where T : Component
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            return child.AddComponent<T>();
        }
    }
}
