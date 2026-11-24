using System;
using UnityEngine;

namespace DeadLine
{
    [CreateAssetMenu(fileName = "MovementData", menuName = "DeadLine/Character/Movement Data", order = 1)]
    public class MovementData : ScriptableObject
    {
        [Header("=== SPEEDS ===")]
        [SerializeField][Min(0f)] public float MaxWalkSpeed = 5f;
        [SerializeField][Min(0f)] public float MaxAirSpeed = 1f;
        [SerializeField][Min(0f)] public float MaxRunSpeed = 10f;
        [SerializeField][Min(0f)] public float MaxCrouchSpeed = 2f;
        [SerializeField][Min(0f)] public float MaxWalkSpeedWhilePushing = 2f;
        [SerializeField][Min(0f)] public float ToCrouchingSpeed = 1.5f;
        [HideInInspector] public float MaxOverallSpeed => Mathf.Max(new float[]{MaxWalkSpeed, MaxAirSpeed, MaxRunSpeed, MaxCrouchSpeed, MaxWalkSpeedWhilePushing});
        
        [Header("=== ACCELERATIONS ===")]
        [SerializeField][Min(0f)] public float Acceleration = 20f;
        [SerializeField][Min(0f)] public float Deceleration = 25f;
        [SerializeField][Min(0f)] public float RunningAcceleration = 30f;
        [SerializeField][Min(0f)] public float RunningDeceleration = 37f;
        [SerializeField][Min(0f)] public float AirAcceleration = 6;
        [SerializeField][Min(0f)] public float AirDeceleration = 9.5f;
        [SerializeField][Min(0f)] public float CrouchAcceleration = 16f;
        [SerializeField][Min(0f)] public float CrouchDeceleration = 20f;
        [SerializeField][Min(0f)] public float WhilePushingAcceleration = 6f;
        [SerializeField][Min(0f)] public float WhilePushingDeceleration = 9.5f;
        

        [Header("=== JUMP SETTINGS ===")] 
        [Tooltip("Altura máxima alcanzada manteniendo el botón el tiempo completo")]
        [SerializeField] [Min(0.1f)] public float MaxJumpHeight = 8f;

        [Tooltip("Segundos que tarda el jugador en llegar al punto mas alto tras saltar")]
        [SerializeField] [Min(0.1f)] public float TimeToApex = 0.7f;

        [Header("=== HEIGHTS ===")] 
        [SerializeField] public float Height;
        [SerializeField] public float CrouchHeight;

        [Header("=== COLLISIONS ===")] 
        [SerializeField] public float CharacterRadius;
        [SerializeField][Min(0.01f)] public float SkinWidth;


        [Header("=== GRAVITY & AIR ===")] 
        [Tooltip("Multiplicador de gravedad en la caida")]
        [SerializeField][Min(1)] public float FallGravityMultiply;
        [Tooltip("Gravedad aplicada en el suelo, para que se mantenga en el suelo")]
        [SerializeField] public float GroundedGravity;
        [SerializeField] public float MaxFallSpeed;

        [Header("=== GROUND CHECKING ===")]
        [SerializeField] public float SphereCastRadius = 0.35f;
        [SerializeField] public float SphereDistance = 0.15f;
        [SerializeField] public LayerMask GroundLayer;

        [Header("=== STEP/STAIRS CLIMBING ===")]
        [Tooltip("Altura máxima que el personaje puede subir automáticamente (escalones/escaleras)")]
        [SerializeField][Min(0f)] public float MaxStepHeight = 0.5f;
        [Tooltip("Distancia hacia adelante para detectar escalones")]
        [SerializeField][Min(0.1f)] public float StepDetectionDistance = 0.5f;
        [Tooltip("Velocidad de ajuste vertical al subir escalones")]
        [SerializeField][Min(1f)] public float StepClimbSpeed = 10f;

        [Header("=== LADDER CLIMBING ===")]
        [Tooltip("Velocidad de subida/bajada en escaleras de mano")]
        [SerializeField][Min(0f)] public float LadderClimbSpeed = 3f;
        [Tooltip("Aceleración al moverse en escaleras")]
        [SerializeField][Min(0f)] public float LadderClimbAcceleration = 10f;
        [Tooltip("Rango máximo de movimiento lateral en la escalera (ancho total)")]
        [SerializeField][Min(0f)] public float LadderLateralRange = 1f;

        public float GetSpeed(bool running, bool crouching)
        {
            if (crouching) return MaxCrouchSpeed;
            if (running) return MaxRunSpeed;
            return MaxWalkSpeed;
        }

        void OnValidate()
        {
            Height = Math.Max(0, Height);
            CrouchHeight = Math.Min(Math.Max(0, CrouchHeight), Height);

            MaxRunSpeed = Math.Max(MaxWalkSpeed, MaxRunSpeed);
        }
    }
}