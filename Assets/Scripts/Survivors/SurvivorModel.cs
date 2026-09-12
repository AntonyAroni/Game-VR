using System.Collections.Generic;
using UnityEngine;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.Survivors.Symptoms;

namespace ZombieCheckpoint.Survivors
{
    /// <summary>
    /// Modelo y controlador principal del superviviente que ingresa al puesto de inspección.
    /// Principio SRP: Gestiona el estado de infección, sus síntomas asociados y las animaciones de llegada/salida.
    /// </summary>
    public class SurvivorModel : MonoBehaviour
    {
        [Header("Datos de Identidad")]
        [SerializeField] private string survivorName = "Sujeto de Prueba";
        [SerializeField] private int age = 28;
        [SerializeField] private bool isTrulyInfected = false;

        [Header("Referencias a Síntomas")]
        [SerializeField] private BiteMarkSymptom biteSymptom;
        [SerializeField] private HeartbeatSymptom heartbeatSymptom;
        [SerializeField] private PupilSymptom pupilSymptom;

        [Header("Animador (Mixamo Rig)")]
        [SerializeField] private Animator animator;
        
        // Hashes de parámetros de animación para optimización
        private static readonly int WalkParam = Animator.StringToHash("IsWalking");
        private static readonly int PanicParam = Animator.StringToHash("IsPanicking");

        public string SurvivorName => survivorName;
        public int Age => age;
        public bool IsTrulyInfected => isTrulyInfected;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (biteSymptom == null) biteSymptom = GetComponentInChildren<BiteMarkSymptom>(true);
            if (heartbeatSymptom == null) heartbeatSymptom = GetComponentInChildren<HeartbeatSymptom>(true);
            if (pupilSymptom == null) pupilSymptom = GetComponentInChildren<PupilSymptom>(true);
        }

        /// <summary>
        /// Configura el superviviente de forma procedural para una ronda de inspección.
        /// </summary>
        public void SetupProfile(string name, int ageVal, bool infected, bool hasBiteMark, bool hasAbnormalHeartbeat, bool hasAbnormalPupil = false)
        {
            survivorName = name;
            age = ageVal;
            isTrulyInfected = infected;

            if (biteSymptom != null)
            {
                biteSymptom.Initialize(hasBiteMark);
            }

            if (heartbeatSymptom != null)
            {
                heartbeatSymptom.Initialize(hasAbnormalHeartbeat);
            }

            if (pupilSymptom != null)
            {
                pupilSymptom.Initialize(hasAbnormalPupil);
            }
        }

        public void PlayArrivalAnimation()
        {
            if (animator != null)
            {
                animator.SetBool(WalkParam, true);
            }
        }

        public void PlayStationaryIdle()
        {
            if (animator != null)
            {
                animator.SetBool(WalkParam, false);
            }
        }

        public void Depart(VerdictType verdict)
        {
            if (animator != null)
            {
                animator.SetBool(WalkParam, true);
                if (verdict == VerdictType.SendToQuarantine)
                {
                    animator.SetBool(PanicParam, true);
                }
            }
        }
    }
}
