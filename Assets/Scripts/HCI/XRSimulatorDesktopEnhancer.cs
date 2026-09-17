#pragma warning disable CS0618
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace ZombieCheckpoint.HCI
{
    /// <summary>
    /// Optimizador ergonómico para el simulador XR en entorno de escritorio (Teclado + Ratón).
    /// Principio de IHC: Mapeo Natural, Visibilidad y Reducción de Carga Cognitiva.
    /// - Activa automáticamente el control de la mano derecha en modo traslación 3D al iniciar.
    /// - Inclina ergonómicamente el mando hacia la mesa (30°) para que el rayo apunte directo a herramientas y documentos.
    /// - Permite rotar el cabezal/visor con Alt Izquierdo, Ctrl Izquierdo o botón central del ratón.
    /// - Tecla T para alternar inclinación de mesa (35° / 0°).
    /// - Teclas Q/E para ajuste milimétrico de inclinación vertical del rayo.
    /// - Permite mirar alrededor manteniendo pulsado el Clic Derecho (estilo FPS).
    /// - Permite agarrar e interactuar directamente con Clic Izquierdo.
    /// </summary>
    public class XRSimulatorDesktopEnhancer : MonoBehaviour
    {
        [Header("Configuración del Simulador")]
        [SerializeField] private XRDeviceSimulator simulator;
        [SerializeField] private bool autoTargetRightHandOnStart = true;
        [SerializeField] private bool autoTiltToTableOnStart = true;
        [SerializeField] private bool showHelpOverlay = true;

        private FieldInfo targetDeviceField;
        private FieldInfo rotateOverrideField;
        private FieldInfo rightEulerField;
        private FieldInfo leftEulerField;
        private FieldInfo rightStateField;
        private FieldInfo leftStateField;

        // Soporte de reflexión para manos articuladas (XRSimulatedHandState)
        private FieldInfo rightHandStateField;
        private FieldInfo leftHandStateField;
        private PropertyInfo handEulerProp;
        private PropertyInfo handRotationProp;

        private object rightDeviceEnumValue;
        private object leftDeviceEnumValue;
        private object fpsEnumValue;
        private bool isLookingWithCamera = false;
        private bool isRightHandActive = true;
        private bool isTiltedToTable = true;

        private static readonly string HelpText = 
            "• <b>Mover Ratón:</b> Mover mano activa en 3D\n" +
            "• <b>Clic Izquierdo:</b> AGARRAR / USAR herramienta\n" +
            "• <b>Alt Izq (o Ctrl) + Ratón:</b> GIRAR muñeca / rayo (1:1)\n" +
            "• <b>T:</b> Alternar inclinación mesa (35° / 0°)\n" +
            "• <b>Q / E:</b> Subir / Bajar inclinación rayo (±5°)\n" +
            "• <b>R:</b> Conmutar permanente Traslación / Rotación\n" +
            "• <b>Clic Derecho (Mantener):</b> Mirar con la cámara\n" +
            "• <b>Rueda Ratón:</b> Acercar / Alejar profundidad\n" +
            "• <b>Tab:</b> Alternar Mano Derecha / Izquierda\n" +
            "• <b>V (o B):</b> Ordenar Levantar / Bajar brazos (Civil)\n" +
            "• <b>C:</b> Retirar / Levantar polo (Torso)\n" +
            "• <b>H:</b> Conmutar Modo Mando / Manos (Simulator)\n" +
            "• <b>F1 / O:</b> Ocultar / Mostrar esta ayuda";

        private GUIStyle boxStyle;
        private GUIStyle textStyle;

        private void Awake()
        {
            if (simulator == null)
            {
                if (!TryGetComponent(out simulator)) simulator = FindAnyObjectByType<XRDeviceSimulator>();
            }

            if (simulator != null)
            {
                CacheReflectionMembers();
            }
        }

        private void CacheReflectionMembers()
        {
            Type simType = typeof(XRDeviceSimulator);
            targetDeviceField = simType.GetField("m_TargetedDeviceInput", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetDeviceField != null)
            {
                Type enumType = targetDeviceField.FieldType;
                rightDeviceEnumValue = Enum.Parse(enumType, "RightDevice");
                leftDeviceEnumValue = Enum.Parse(enumType, "LeftDevice");
                fpsEnumValue = Enum.Parse(enumType, "FPS");
            }

            rotateOverrideField = simType.GetField("m_RotateModeOverrideInput", BindingFlags.NonPublic | BindingFlags.Instance);
            rightEulerField = simType.GetField("m_RightControllerEuler", BindingFlags.NonPublic | BindingFlags.Instance);
            leftEulerField = simType.GetField("m_LeftControllerEuler", BindingFlags.NonPublic | BindingFlags.Instance);
            rightStateField = simType.GetField("m_RightControllerState", BindingFlags.NonPublic | BindingFlags.Instance);
            leftStateField = simType.GetField("m_LeftControllerState", BindingFlags.NonPublic | BindingFlags.Instance);

            // Reflexión de manos articuladas
            rightHandStateField = simType.GetField("m_RightHandState", BindingFlags.NonPublic | BindingFlags.Instance);
            leftHandStateField = simType.GetField("m_LeftHandState", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rightHandStateField != null)
            {
                Type handStateType = rightHandStateField.FieldType;
                handEulerProp = handStateType.GetProperty("euler");
                handRotationProp = handStateType.GetProperty("rotation");
            }
        }

        private void Start()
        {
            if (simulator == null) return;

            simulator.mouseTransformationMode = XRDeviceSimulator.TransformationMode.Translate;

            // Sensibilidad óptima de rotación (1:1) para que el puntero/rayo responda con agilidad
            simulator.mouseXRotateSensitivity = 1.0f;
            simulator.mouseYRotateSensitivity = 1.0f;

            OptimizeRigInteractors();

            if (autoTargetRightHandOnStart)
            {
                SetTargetDevice(true);
            }

            if (autoTiltToTableOnStart)
            {
                SetControllerPitch(30f);
            }
        }

        private void OptimizeRigInteractors()
        {
            // 1. Desactivar el filtro post-procesador de manos que causa latencia/amortiguamiento en PC
            var filter = FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.Samples.Hands.HandsOneEuroFilterPostProcessor>();
            if (filter != null) filter.enabled = false;

            // 2. Eliminar estabilización artificial excesiva del puntero para respuesta instantánea
            var nearFars = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(FindObjectsSortMode.None);
            foreach (var nf in nearFars)
            {
                var attach = nf.GetComponent<UnityEngine.XR.Interaction.Toolkit.Attachment.InteractionAttachController>();
                var caster = nf.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.CurveInteractionCaster>();
                if (attach != null)
                {
                    attach.angleStabilization = 0f;
                    attach.positionStabilization = 0f;
                    attach.smoothOffset = false;
                }
                if (caster != null)
                {
                    caster.enableStabilization = false;
                }
            }
        }

        private void Update()
        {
            if (simulator == null || targetDeviceField == null) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;

            // 1. Mirar alrededor con Clic Derecho (mantener presionado)
            if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    isLookingWithCamera = true;
                    targetDeviceField.SetValue(simulator, fpsEnumValue);
                }
                else if (mouse.rightButton.wasReleasedThisFrame)
                {
                    isLookingWithCamera = false;
                    SetTargetDevice(isRightHandActive);
                }
            }

            // Si está mirando con la cámara, no procesar alternancias de manos
            if (isLookingWithCamera) return;

            // 2. Rotación libre manteniendo Alt Izquierdo, Ctrl Izquierdo o Botón Central del Ratón
            bool holdRotate = (keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.leftCtrlKey.isPressed)) ||
                              (mouse != null && mouse.middleButton.isPressed);

            if (rotateOverrideField != null && holdRotate)
            {
                rotateOverrideField.SetValue(simulator, true);
            }

            if (keyboard != null)
            {
                // 3. Tecla T: Alternar inclinación directa hacia la mesa de trabajo (35° / 0°)
                if (keyboard.tKey.wasPressedThisFrame)
                {
                    isTiltedToTable = !isTiltedToTable;
                    SetControllerPitch(isTiltedToTable ? 35f : 0f);
                }

                // 4. Teclas Q y E: Ajuste fino de inclinación vertical del rayo (±5°)
                if (keyboard.qKey.wasPressedThisFrame)
                {
                    AdjustControllerPitch(-5f);
                }
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    AdjustControllerPitch(5f);
                }

                // 5. Alternar entre Mano Derecha e Izquierda con TAB
                if (keyboard.tabKey.wasPressedThisFrame)
                {
                    isRightHandActive = !isRightHandActive;
                    SetTargetDevice(isRightHandActive);
                }

                // 6. Alternar visibilidad de la ayuda con F1, O o H
                if (keyboard.f1Key.wasPressedThisFrame || keyboard.oKey.wasPressedThisFrame || keyboard.hKey.wasPressedThisFrame)
                {
                    showHelpOverlay = !showHelpOverlay;
                }
            }
        }

        private void SetTargetDevice(bool rightHand)
        {
            if (simulator == null || targetDeviceField == null) return;

            object targetVal = rightHand ? rightDeviceEnumValue : leftDeviceEnumValue;
            targetDeviceField.SetValue(simulator, targetVal);
            simulator.mouseTransformationMode = XRDeviceSimulator.TransformationMode.Translate;
        }

        private void SetControllerPitch(float targetPitch)
        {
            if (simulator == null) return;

            // 1. Inclinación para modo Mandos
            FieldInfo eulerField = isRightHandActive ? rightEulerField : leftEulerField;
            FieldInfo stateField = isRightHandActive ? rightStateField : leftStateField;

            if (eulerField != null && stateField != null)
            {
                Vector3 curEuler = (Vector3)eulerField.GetValue(simulator);
                curEuler.x = targetPitch;
                eulerField.SetValue(simulator, curEuler);

                object stateObj = stateField.GetValue(simulator);
                if (stateObj != null)
                {
                    var rotField = stateObj.GetType().GetField("deviceRotation");
                    if (rotField != null)
                    {
                        rotField.SetValue(stateObj, Quaternion.Euler(curEuler));
                        stateField.SetValue(simulator, stateObj);
                    }
                }
            }

            // 2. Inclinación para modo Manos Articuladas
            FieldInfo handStateField = isRightHandActive ? rightHandStateField : leftHandStateField;
            if (handStateField != null && handEulerProp != null && handRotationProp != null)
            {
                object handStateObj = handStateField.GetValue(simulator);
                if (handStateObj != null)
                {
                    Vector3 hEuler = (Vector3)handEulerProp.GetValue(handStateObj, null);
                    hEuler.x = targetPitch;
                    handEulerProp.SetValue(handStateObj, hEuler, null);
                    handRotationProp.SetValue(handStateObj, Quaternion.Euler(hEuler), null);
                    handStateField.SetValue(simulator, handStateObj);
                }
            }
        }

        private void AdjustControllerPitch(float deltaPitch)
        {
            if (simulator == null) return;

            // 1. Ajuste fino en modo Mandos
            FieldInfo eulerField = isRightHandActive ? rightEulerField : leftEulerField;
            FieldInfo stateField = isRightHandActive ? rightStateField : leftStateField;

            if (eulerField != null && stateField != null)
            {
                Vector3 curEuler = (Vector3)eulerField.GetValue(simulator);
                curEuler.x = Mathf.Clamp(curEuler.x + deltaPitch, -85f, 85f);
                eulerField.SetValue(simulator, curEuler);

                object stateObj = stateField.GetValue(simulator);
                if (stateObj != null)
                {
                    var rotField = stateObj.GetType().GetField("deviceRotation");
                    if (rotField != null)
                    {
                        rotField.SetValue(stateObj, Quaternion.Euler(curEuler));
                        stateField.SetValue(simulator, stateObj);
                    }
                }
            }

            // 2. Ajuste fino en modo Manos Articuladas
            FieldInfo handStateField = isRightHandActive ? rightHandStateField : leftHandStateField;
            if (handStateField != null && handEulerProp != null && handRotationProp != null)
            {
                object handStateObj = handStateField.GetValue(simulator);
                if (handStateObj != null)
                {
                    Vector3 hEuler = (Vector3)handEulerProp.GetValue(handStateObj, null);
                    hEuler.x = Mathf.Clamp(hEuler.x + deltaPitch, -85f, 85f);
                    handEulerProp.SetValue(handStateObj, hEuler, null);
                    handRotationProp.SetValue(handStateObj, Quaternion.Euler(hEuler), null);
                    handStateField.SetValue(simulator, handStateObj);
                }
            }
        }

        private float GetActiveControllerPitch()
        {
            if (simulator == null) return 0f;

            FieldInfo handStateField = isRightHandActive ? rightHandStateField : leftHandStateField;
            if (handStateField != null && handEulerProp != null)
            {
                object handStateObj = handStateField.GetValue(simulator);
                if (handStateObj != null)
                {
                    Vector3 hEuler = (Vector3)handEulerProp.GetValue(handStateObj, null);
                    if (Mathf.Abs(hEuler.x) > 0.01f) return hEuler.x;
                }
            }

            FieldInfo eulerField = isRightHandActive ? rightEulerField : leftEulerField;
            if (eulerField != null)
            {
                Vector3 euler = (Vector3)eulerField.GetValue(simulator);
                return euler.x;
            }
            return 0f;
        }

        private void OnGUI()
        {
            if (!showHelpOverlay) return;

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.background = Texture2D.blackTexture;
            }

            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true,
                    richText = true
                };
                textStyle.normal.textColor = Color.white;
            }

            Color prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Box(new Rect(15, Screen.height - 215, 340, 200), GUIContent.none);
            GUI.color = prevColor;

            GUILayout.BeginArea(new Rect(25, Screen.height - 210, 320, 190));
            
            string handIndicator;
            if (isLookingWithCamera)
            {
                handIndicator = "<color=#ffcc00>👁️ Modo Cámara (Mirar)</color>";
            }
            else
            {
                string handStr = isRightHandActive ? "<color=#33ccff>🖐️ Mano Der</color>" : "<color=#ff66cc>🖐️ Mano Izq</color>";
                bool isRotate = simulator != null && simulator.mouseTransformationMode == XRDeviceSimulator.TransformationMode.Rotate;
                string modeStr = isRotate ? "<color=#ff9900>[ROTACIÓN]</color>" : "<color=#00ff88>[TRASLACIÓN]</color>";
                float pitch = GetActiveControllerPitch();
                handIndicator = $"{handStr} | {modeStr} | Inclinación: {pitch:F0}°";
            }

            GUILayout.Label($"<b>CONTROLES DE SIMULADOR (PC)</b>\n{handIndicator}\n{HelpText}", textStyle);
            GUILayout.EndArea();
        }
    }
}
