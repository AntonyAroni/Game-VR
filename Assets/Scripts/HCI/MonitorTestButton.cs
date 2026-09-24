using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;

namespace ZombieCheckpoint.HCI
{
    /// <summary>
    /// Botón interactivo diegético sobre el chasis del monitor cardíaco.
    /// Permite al oficial comprobar el audio y el trazado de pulso del monitor.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MonitorTestButton : MonoBehaviour
    {
        [SerializeField] private VitalSignsMonitor monitor;
        private float lastPressTime = -10f;

        private void Awake()
        {
            if (monitor == null)
            {
                monitor = GetComponentInParent<VitalSignsMonitor>();
            }
        }

        private void OnMouseDown()
        {
            PressButton();
        }

        private void OnTriggerEnter(Collider other)
        {
            PressButton();
        }

        public void PressButton()
        {
            if (Time.time - lastPressTime < 1.0f) return;
            lastPressTime = Time.time;

            if (monitor != null)
            {
                monitor.TriggerTestPulse();
            }
        }
    }
}
