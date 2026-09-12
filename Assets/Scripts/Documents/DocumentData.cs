using System;
using UnityEngine;

namespace ZombieCheckpoint.Documents
{
    /// <summary>
    /// Datos del documento de identidad y salvoconducto sanitario.
    /// Principio SRP: Modelo de datos puro para desacoplar el estado de la vista gráfica.
    /// </summary>
    [Serializable]
    public class DocumentData
    {
        public string holderName = "John Doe";
        public int holderAge = 32;
        public string documentId = "BIO-4921-X";
        public string expirationDate = "15/09/2026";
        public string bloodType = "O+";
        public bool isFalsified = false;
        public string falsificationDetail = "";

        public static DocumentData CreateRandomValid(string name, int age)
        {
            return new DocumentData
            {
                holderName = name,
                holderAge = age,
                documentId = $"BIO-{UnityEngine.Random.Range(1000, 9999)}-Z",
                expirationDate = "20/12/2026",
                bloodType = "A+",
                isFalsified = false
            };
        }

        public static DocumentData CreateFalsifiedExpired(string name, int age)
        {
            return new DocumentData
            {
                holderName = name,
                holderAge = age,
                documentId = $"BIO-{UnityEngine.Random.Range(1000, 9999)}-INVALID",
                expirationDate = "01/01/2024", // Vencido
                bloodType = "AB-",
                isFalsified = true,
                falsificationDetail = "Documento vencido hace más de 2 años."
            };
        }
    }
}
