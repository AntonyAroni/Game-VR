using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI
{
    /// <summary>
    /// Administrador de anclaje y recentrado del evaluador en el puesto de inspección de la cabina.
    /// Principio de IHC: Mapeo Natural, Ergonomía y Estabilidad Espacial.
    /// Resuelve el problema de desfase del área de juego de Meta Quest (Guardian):
    /// - Al iniciar la app, calibra y alinea automáticamente al jugador en el puesto exacto frente al mostrador.
    /// - Mantiene la altura natural de los ojos del usuario sobre el suelo (Floor Tracking).
    /// - Se suscribe a los eventos de recentrado de Meta Quest (botón Oculus/Meta presionado)
    ///   para reubicarlo de inmediato en el puesto de inspección.
    /// - Implementa salvaguarda continua: si el usuario se aleja accidentalmente o cae fuera de la cabina,
    ///   lo reubica suavemente frente a la mesa de inspección.
    /// - Desactiva la gravedad artificial del rig VR para evitar caídas al vacío en cabinas estáticas.
    /// </summary>
    public class PlayerStationManager : MonoBehaviour
    {
        public static PlayerStationManager Instance { get; private set; }

        [Header("Referencias de XR Origin")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Transform stationAnchor;

        [Header("Configuración de Calibración")]
        [SerializeField] private Vector3 defaultStationPosition = new Vector3(0f, 0f, 0.05f);
        [SerializeField] private Vector3 defaultStationForward = Vector3.forward;
        [SerializeField] private float outOfBoundsDistance = 1.6f;
        [SerializeField] private float minimumFloorHeight = 0.2f;

        private Camera xrCamera;
        private CharacterController characterController;
        private MonoBehaviour gravityProvider;
        private Coroutine calibrationRoutine;
        private float outOfBoundsTimer = 0f;
        private readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            ResolveReferences();
        }

        private void OnEnable()
        {
            SubscribeToSubsystems();
        }

        private void OnDisable()
        {
            UnsubscribeFromSubsystems();
        }

        private void SubscribeToSubsystems()
        {
            SubsystemManager.GetSubsystems(inputSubsystems);
            for (int i = 0; i < inputSubsystems.Count; i++)
            {
                var sub = inputSubsystems[i];
                if (sub != null)
                {
                    sub.trackingOriginUpdated -= HandleTrackingOriginUpdated;
                    sub.trackingOriginUpdated += HandleTrackingOriginUpdated;
                }
            }
        }

        private void UnsubscribeFromSubsystems()
        {
            for (int i = 0; i < inputSubsystems.Count; i++)
            {
                var sub = inputSubsystems[i];
                if (sub != null)
                {
                    sub.trackingOriginUpdated -= HandleTrackingOriginUpdated;
                }
            }
            inputSubsystems.Clear();
        }

        private void Start()
        {
            ResolveReferences();
            DisableVirtualGravity();

            // Calibrar automáticamente en cuanto el subsistema VR reporte el pose de la cabeza
            if (calibrationRoutine != null) StopCoroutine(calibrationRoutine);
            calibrationRoutine = StartCoroutine(InitialCalibrationRoutine());
        }

        private void ResolveReferences()
        {
            if (xrOrigin == null)
            {
                var rigObj = GameObject.Find("XR Origin Hands (XR Rig)") ?? GameObject.Find("XR Origin (XR Rig)");
                if (rigObj != null)
                {
                    xrOrigin = rigObj.GetComponent<XROrigin>();
                }
                else
                {
                    xrOrigin = FindAnyObjectByType<XROrigin>();
                }
            }

            if (xrOrigin != null)
            {
                xrCamera = xrOrigin.Camera;
                characterController = xrOrigin.GetComponent<CharacterController>();

                // Buscar GravityProvider si existe en los hijos de Locomotion
                foreach (var mb in xrOrigin.GetComponentsInChildren<MonoBehaviour>())
                {
                    if (mb != null && mb.GetType().Name.Contains("Gravity"))
                    {
                        gravityProvider = mb;
                        break;
                    }
                }
            }

            if (stationAnchor == null)
            {
                var anchorObj = GameObject.Find("Player_Station_Anchor");
                if (anchorObj != null)
                {
                    stationAnchor = anchorObj.transform;
                }
            }
        }

        private void DisableVirtualGravity()
        {
            // En una cabina de inspección estática en VR con tracking de suelo,
            // la gravedad artificial de CharacterController no es necesaria y genera
            // caídas al vacío si el usuario se desvía de los colisionadores de la sala.
            if (gravityProvider != null)
            {
                gravityProvider.enabled = false;
                Debug.Log("[PlayerStationManager] ✅ GravityProvider desactivado para evitar caídas al vacío.");
            }

            if (characterController != null)
            {
                // Configurar radio mínimo para no empujar la cámara por colisión contra muebles
                characterController.radius = 0.05f;
            }
        }

        private IEnumerator InitialCalibrationRoutine()
        {
            // Esperar unos cuadros a que OpenXR / XR Hands inicialicen el tracking 6DOF
            yield return new WaitForSeconds(0.1f);

            int maxAttempts = 30; // 30 intentos a 0.05s = 1.5s máx
            while (maxAttempts > 0)
            {
                if (xrCamera != null && xrCamera.transform.position.sqrMagnitude > 0.0001f)
                {
                    break;
                }
                yield return new WaitForSeconds(0.05f);
                maxAttempts--;
            }

            RecenterToStation(forceForwardYaw: true);
            Debug.Log("[PlayerStationManager] ✅ Calibración inicial completada frente al mostrador.");

            // Repetir a los 0.5s para asegurar compensación tras estabilización de Meta Guardian
            yield return new WaitForSeconds(0.4f);
            RecenterToStation(forceForwardYaw: true);
        }

        private void HandleTrackingOriginUpdated(XRInputSubsystem subsystem)
        {
            // Recentrado nativo gatillado por el menú del sistema de Meta Quest
            Debug.Log("[PlayerStationManager] Recibido evento trackingOriginUpdated de OpenXR. Re-alineando a la mesa...");
            RecenterToStation(forceForwardYaw: true);
        }

        /// <summary>
        /// Re-alinea inmediatamente la cámara del jugador en el puesto de inspección frente al mostrador.
        /// Preserva la altura natural del usuario sobre el suelo.
        /// </summary>
        /// <param name="forceForwardYaw">Si es true, orienta la mirada directamente hacia la ventanilla/civil (+Z).</param>
        public void RecenterToStation(bool forceForwardYaw = true)
        {
            if (xrOrigin == null)
            {
                ResolveReferences();
                if (xrOrigin == null) return;
            }

            Vector3 targetPos = stationAnchor != null ? stationAnchor.position : defaultStationPosition;
            Vector3 targetForward = stationAnchor != null ? stationAnchor.forward : defaultStationForward;

            Camera cam = xrOrigin.Camera != null ? xrOrigin.Camera : xrCamera;
            if (cam == null) return;

            // 1. Orientación del cabezal hacia el frente del mostrador
            if (forceForwardYaw)
            {
                xrOrigin.MatchOriginUpCameraForward(Vector3.up, targetForward);
            }

            // 2. Traslación del Origin para que la cámara coincida exactamente con la posición horizontal del ancla
            float eyeY = cam.transform.position.y;
            // Asegurar altura mínima de confort visual sobre la mesa
            if (eyeY < minimumFloorHeight) eyeY = 1.60f;

            Vector3 desiredCameraWorld = new Vector3(targetPos.x, eyeY, targetPos.z);
            xrOrigin.MoveCameraToWorldLocation(desiredCameraWorld);

            // 3. Notificación háptica y auditiva sutil de confirmación
            EventBus.RequestSpatialAudio("button_click", desiredCameraWorld, 0.4f);
            EventBus.RequestHapticImpulse(HandSide.Both, 0.35f, 0.08f);

            Debug.Log($"[PlayerStationManager] Puesto de inspección calibrado en: {cam.transform.position}");
        }

        private void Update()
        {
            // Re-vincular subsistemas si no estaban listos en OnEnable
            if (inputSubsystems.Count == 0 && Time.frameCount % 60 == 0)
            {
                SubscribeToSubsystems();
            }

            if (xrOrigin == null || xrCamera == null) return;

            Vector3 stationPos = stationAnchor != null ? stationAnchor.position : defaultStationPosition;
            Vector3 camPos = xrCamera.transform.position;

            // Medir distancia horizontal con el puesto
            float horizDistSqr = (camPos.x - stationPos.x) * (camPos.x - stationPos.x) + 
                                 (camPos.z - stationPos.z) * (camPos.z - stationPos.z);

            bool isOutOfBounds = (horizDistSqr > (outOfBoundsDistance * outOfBoundsDistance)) || (camPos.y < minimumFloorHeight);

            if (isOutOfBounds)
            {
                outOfBoundsTimer += Time.deltaTime;
                // Si pasa más de 1.2 segundos fuera de límites o cae bajo el suelo, re-posicionar
                if (outOfBoundsTimer > 1.2f || camPos.y < -0.1f)
                {
                    Debug.LogWarning($"[PlayerStationManager] Jugador fuera de límites ({camPos}). Re-centrando automáticamente.");
                    RecenterToStation(forceForwardYaw: false);
                    outOfBoundsTimer = 0f;
                }
            }
            else
            {
                outOfBoundsTimer = 0f;
            }
        }
    }
}
