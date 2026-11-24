
using System.Collections.Generic;
using UnityEngine;

namespace DeadLine
{
    /// <summary>
    /// Detector de proximidad para detectar interactuables cercanos usando triggers.
    /// Similar a GroundDetector pero para interacciones por proximidad.
    /// </summary>
    public class ProximityDetector : BaseDetector
    {
        private readonly HashSet<InteractableObjectsBase> _candidates = new();
        
        /// <summary>
        /// Registra un objeto interactuable cuando entra en el área de proximidad
        /// </summary>
        public void Register(InteractableObjectsBase interactableObject)
        {
            if (interactableObject != null)
                _candidates.Add(interactableObject);
        }

        /// <summary>
        /// Desregistra un objeto interactuable cuando sale del área de proximidad
        /// </summary>
        public void Unregister(InteractableObjectsBase interactableObject)
        {
            if (interactableObject != null)
                _candidates.Remove(interactableObject);
        }

        /// <summary>
        /// Obtiene todos los candidatos de interacción en proximidad
        /// </summary>
        public HashSet<InteractableObjectsBase> GetCandidates()
        {
            // Limpiar nulos
            _candidates.RemoveWhere(interactableObject => interactableObject == null);
            return _candidates;
        }

        /// <summary>
        /// Verifica si hay al menos un candidato disponible
        /// </summary>
        public bool HasCandidates()
        {
            _candidates.RemoveWhere(interactableObject => interactableObject == null);
            return _candidates.Count > 0;
        }

        /// <summary>
        /// Limpia todos los candidatos registrados
        /// </summary>
        public void ClearCandidates()
        {
            _candidates.Clear();
        }

        protected override void OnDrawGizmos()
        {
        }
    }
}
