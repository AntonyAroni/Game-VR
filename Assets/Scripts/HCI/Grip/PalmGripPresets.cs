using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.Documents;
using ZombieCheckpoint.Tools;

namespace ZombieCheckpoint.HCI.Grip
{
    /// <summary>
    /// Asientos en la palma calibrados para cada herramienta del puesto de control.
    /// Principio de Responsabilidad Única (SRP): sólo traduce "qué objeto es" en "cómo se empuña".
    /// Las partes del cuerpo del civil no reciben perfil a propósito: su agarre por pinza
    /// alimenta la cinemática del brazo y no debe alterarse.
    ///
    /// Los ejes locales proceden de la geometría que genera <c>CheckpointSceneBuilder</c>:
    /// - Linterna: cápsula en Y, foco en +Y (haz hacia +Y).
    /// - Sello: vástago vertical en Y con la perilla arriba y la almohadilla abajo.
    /// - Estetoscopio: campana en el hueso "Bell"; el cuerpo se extiende hacia el centro del BoxCollider.
    /// - Pasaporte: cara legible en +Y y texto orientado hacia +Z.
    /// </summary>
    public static class PalmGripPresets
    {
        /// <summary>
        /// Añade y configura un <see cref="PalmGripProfile"/> si el objeto es una herramienta
        /// o documento conocido. Devuelve <c>false</c> si ya tenía perfil o no aplica.
        /// </summary>
        public static bool TryApply(XRGrabInteractable grab)
        {
            if (grab == null || grab.TryGetComponent(out PalmGripProfile _)) return false;

            if (grab.TryGetComponent(out FlashlightTool _))
            {
                grab.gameObject.GetOrAddComponent<DirectInteractionOnlyFilter>();
                ApplyPowerGrip(grab, Vector3.up, HandAxis.ThumbSide, Vector3.forward, HandAxis.PalmOut,
                               handleBias: -0.2f, radiusMeters: 0.02f);
                return true;
            }

            if (grab.TryGetComponent(out StampTool _))
            {
                grab.gameObject.GetOrAddComponent<DirectInteractionOnlyFilter>();
                ApplyPowerGrip(grab, Vector3.up, HandAxis.ThumbSide, Vector3.forward, HandAxis.FingersForward,
                               handleBias: 0f, radiusMeters: 0.013f);
                return true;
            }

            if (grab.TryGetComponent(out StethoscopeTool _))
            {
                grab.gameObject.GetOrAddComponent<DirectInteractionOnlyFilter>();
                ApplyStethoscopeGrip(grab);
                return true;
            }

            if (grab.TryGetComponent(out DocumentInteractable _))
            {
                grab.gameObject.GetOrAddComponent<DirectInteractionOnlyFilter>();
                // Apoyado en la palma, cara hacia fuera y texto hacia los dedos; sujeto por su mitad inferior.
                ApplyPowerGrip(grab, Vector3.forward, HandAxis.FingersForward, Vector3.up, HandAxis.PalmOut,
                               handleBias: -0.25f, radiusMeters: 0.005f);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Empuñadura de fuerza sobre el eje principal del colisionador del objeto.
        /// <paramref name="handleBias"/> desplaza el agarre a lo largo del eje principal
        /// como fracción de la longitud (negativo = hacia la cola, lejos del extremo activo).
        /// </summary>
        private static void ApplyPowerGrip(
            XRGrabInteractable grab,
            Vector3 primaryLocal, HandAxis primaryHand,
            Vector3 secondaryLocal, HandAxis secondaryHand,
            float handleBias, float radiusMeters)
        {
            Vector3 center = Vector3.zero;
            if (TryGetLocalBounds(grab.gameObject, out Bounds bounds))
            {
                Vector3 axis = primaryLocal.normalized;
                float extentAlongAxis = Vector3.Dot(bounds.size, Abs(axis));
                center = bounds.center + axis * (handleBias * extentAlongAxis);
            }

            var profile = grab.gameObject.AddComponent<PalmGripProfile>();
            profile.Configure(null, center, radiusMeters, primaryLocal, primaryHand, secondaryLocal, secondaryHand);
        }

        /// <summary>
        /// El estetoscopio se sostiene por la campana, que es la pieza que toca el tórax:
        /// así basta con apoyar la palma en el pecho del civil para auscultar. El resto del
        /// instrumento sale por el lado del meñique para no atravesar dedos ni antebrazo.
        /// </summary>
        private static void ApplyStethoscopeGrip(XRGrabInteractable grab)
        {
            Transform bell = FindChildByName(grab.transform, "Bell");
            if (bell == null)
            {
                // Modelo de respaldo sin rig: empuñadura genérica por el eje largo.
                ApplyPowerGrip(grab, Vector3.forward, HandAxis.ThumbSide, Vector3.up, HandAxis.PalmIn, 0f, 0.02f);
                return;
            }

            var profile = grab.gameObject.AddComponent<PalmGripProfile>();

            Vector3 bellLocal = grab.transform.InverseTransformPoint(bell.position);
            Vector3 bodyLocal = TryGetLocalBounds(grab.gameObject, out Bounds bounds) ? bounds.center : Vector3.zero;
            Vector3 towardBody = bodyLocal - bellLocal;
            if (towardBody.sqrMagnitude < 1e-6f) towardBody = Vector3.forward;

            profile.Configure(bell, Vector3.zero, 0.02f,
                              towardBody, HandAxis.PinkySide,
                              Vector3.down, HandAxis.PalmOut);
        }

        /// <summary>
        /// Obtiene la caja envolvente del colisionador raíz en espacio local del objeto.
        /// Se prefiere el colisionador a la malla porque es lo que XRI usa para detectar el agarre.
        /// </summary>
        private static bool TryGetLocalBounds(GameObject root, out Bounds local)
        {
            if (root.TryGetComponent(out BoxCollider box))
            {
                local = new Bounds(box.center, box.size);
                return true;
            }

            if (root.TryGetComponent(out CapsuleCollider capsule))
            {
                Vector3 size = Vector3.one * (capsule.radius * 2f);
                size[Mathf.Clamp(capsule.direction, 0, 2)] = Mathf.Max(capsule.height, capsule.radius * 2f);
                local = new Bounds(capsule.center, size);
                return true;
            }

            if (root.TryGetComponent(out SphereCollider sphere))
            {
                local = new Bounds(sphere.center, Vector3.one * (sphere.radius * 2f));
                return true;
            }

            if (root.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
            {
                local = meshFilter.sharedMesh.bounds;
                return true;
            }

            local = default;
            return false;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName) return child;
            }
            return null;
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}
