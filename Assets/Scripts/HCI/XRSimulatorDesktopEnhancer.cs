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

        private object rightDeviceEnumValue;
        private object leftDeviceEnumValue;
        private object fpsEnumValue;
        private bool isLookingWithCamera = false;
        private bool isRightHandActive = true;
        private bool isTiltedToTable = true;

        private static readonly string HelpText = 
            "• <b>Mover Ratón:</b> Mover mano activa en 3D\n" +
            "• <b>Clic Izquierdo:</b> AGARRAR / USAR herramienta\n" +
            "• <b>Alt Izq (o Ctrl) + Ratón:</b> GIRAR cabezal / rayo\n" +
            "• <b>T:</b> Alternar inclinación mesa (35° / 0°)\n" +
            "• <b>Q / E:</b> Subir / Bajar inclinación rayo (±5°)\n" +
            "• <b>R:</b> Conmutar permanente Traslación / Rotación\n" +
            "• <b>Clic Derecho (Mantener):</b> Mirar con la cámara\n" +
            "• <b>Rueda Ratón:</b> Acercar / Alejar profundidad\n" +
            "• <b>Tab:</b> Alternar Mano Derecha / Izquierda\n" +
            "• <b>H:</b> Ocultar / Mostrar esta ayuda";

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
        }

        private void Start()
        {
            if (simulator == null) return;

            simulator.mouseTransformationMode = XRDeviceSimulator.TransformationMode.Translate;

            if (autoTargetRightHandOnStart)
            {
                SetTargetDevice(true);
            }

            if (autoTiltToTableOnStart)
            {
                SetControllerPitch(30f);
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

                // 6. Alternar visibilidad de la ayuda con H
                if (keyboard.hKey.wasPressedThisFrame)
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
        }

        private void AdjustControllerPitch(float deltaPitch)
        {
            if (simulator == null) return;

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
        }

        private float GetActiveControllerPitch()
        {
            if (simulator == null) return 0f;
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
