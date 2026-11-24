using System;
using DeadLine;
using UnityEngine;

namespace DeadLine
{
    [Flags]
    public enum InteractableType { 
        None = 0,
        Push = 1,
        Pull = 2,
        Pickup = 4,
        Ladder = 8,
        Throwable = 16
    }

    /// <summary>
    /// Interfaz para objetos interactuables (empujables, agarrables, etc.)
    /// </summary>
    public interface IInteractable
    {
        InteractableType Type { get; }
        Weight Weight { get; }
        bool CanInteract(Character character);
        void OnInteractStart(Character character);
        void OnInteracting(Character character, Vector3 inputDirection);
        void OnInteractEnd(Character character);
    }

    public enum Weight
    {
        None,
        Light,
        Normal,
        Heavy,
        Immovable
    }

    static class WeightMethods
    {
        /// <summary> Devuelve el valor numérico asociado al peso entre 0 y 1 (0 no pesa nada, 1 el máximo peso) </summary>
        public static float GetValue(this Weight weight)
        {
            switch (weight)
            {
                case Weight.None:
                    return 0f;
                case Weight.Light:
                    return 0.25f;
                case Weight.Normal: default:
                    return 0.5f;
                case Weight.Heavy:
                    return 0.6f;
                case Weight.Immovable:
                    return 1f;
            }

        }
    }
}