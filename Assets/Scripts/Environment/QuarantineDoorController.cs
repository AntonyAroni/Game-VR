using System.Collections;
using UnityEngine;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.Environment
{
    /// <summary>
    /// Controlador físico de la compuerta neumática de cuarentena y descontaminación.
    /// Principio de IHC: Retroalimentación Diegética y Cierre de Acción (Action Closure).
    /// Al decidir "Cuarentena", la puerta se abre con sonido de despresurización de vapor,
    /// permitiendo la entrada del infectado, y se cierra con un cerrojo pesado.
    /// </summary>
    public class QuarantineDoorController : MonoBehaviour
    {
        [Header("Referencias de la Puerta")]
        [SerializeField] private Transform doorLeaf;
        [SerializeField] private float openAngleY = -85f;
        [SerializeField] private float openDuration = 1.0f;
        [SerializeField] private float waitBeforeClose = 2.4f;
        [SerializeField] private float closeDuration = 0.6f;

        private Quaternion closedRotation;
        private Quaternion targetOpenRotation;
        private Coroutine activeDoorRoutine;

        private void Awake()
        {
            if (doorLeaf == null)
            {
                var childDoor = transform.Find("Door");
                if (childDoor != null) doorLeaf = childDoor;
                else doorLeaf = transform;
            }

            closedRotation = doorLeaf.localRotation;
            targetOpenRotation = Quaternion.Euler(0f, openAngleY, 0f);
        }

        private void OnEnable()
        {
            EventBus.OnVerdictSubmitted += HandleVerdict;
        }

        private void OnDisable()
        {
            EventBus.OnVerdictSubmitted -= HandleVerdict;
        }

        private void HandleVerdict(VerdictType verdict)
        {
            if (verdict == VerdictType.SendToQuarantine)
            {
                CycleQuarantineDoor();
            }
        }

        public void CycleQuarantineDoor()
        {
            if (activeDoorRoutine != null)
            {
                StopCoroutine(activeDoorRoutine);
            }
            activeDoorRoutine = StartCoroutine(DoorSequenceRoutine());
        }

        private IEnumerator DoorSequenceRoutine()
        {
            // 1. Despresurización neumática y apertura
            EventBus.RequestSpatialAudio("pneumatic_hiss", transform.position, 1.0f);

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
                doorLeaf.localRotation = Quaternion.Slerp(closedRotation, targetOpenRotation, t);
                yield return null;
            }
            doorLeaf.localRotation = targetOpenRotation;

            // 2. Tiempo para que el sujeto ingrese
            yield return new WaitForSeconds(waitBeforeClose);

            // 3. Cierre mecánico rápido y seguro pesado
            elapsed = 0f;
            while (elapsed < closeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / closeDuration);
                doorLeaf.localRotation = Quaternion.Slerp(targetOpenRotation, closedRotation, t);
                yield return null;
            }
            doorLeaf.localRotation = closedRotation;

            // Golpe de cerrojo final
            EventBus.RequestSpatialAudio("door_heavy_latch", transform.position, 1.0f);
            activeDoorRoutine = null;
        }
    }
}
