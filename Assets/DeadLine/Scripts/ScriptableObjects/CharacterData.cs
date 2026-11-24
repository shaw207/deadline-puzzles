
using System;
using UnityEngine;

namespace DeadLine
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "DeadLine/Character/Character Data", order = 0)]
    public class CharacterData : ScriptableObject
    {
        [Header("===== IDENTITY =====")]
        [SerializeField] public string Name = "Character";
        [SerializeField, TextArea(2, 4)] public string Description = "";
        [SerializeField] public Sprite Icon;

        [Header("===== HEALTH =====")]
        [SerializeField, Range(1f, 1000f)] public float MaxHealth = 100f;
        [SerializeField, Range(0.1f, 100f)] public float HealthRegenRate = 0f;
        [SerializeField, Range(0f, 10f)] public float HealthRegenDelay = 3f;

        [Header("===== COMBAT =====")]
        [SerializeField, Range(1f, 1000f)] public float AttackDamage = 25f;
        [SerializeField, Range(0.1f, 10f)] public float AttackSpeed = 1f;
        [SerializeField, Range(0.5f, 20f)] public float AttackRange = 2f;

        [Header("===== INTERACTION =====")]
        [SerializeField, Range(0.5f, 10f)] public float InteractionRange = 1.5f;
        [SerializeField, Range(0f, 5f)] public float InteractionSpeed = 1f;

        [Header("===== ABILITIES =====")]
        [SerializeField] public bool CanClimb = true;
        [SerializeField] public bool CanSprint = true;
        [SerializeField] public bool CanCrouch = true;
        [SerializeField] public bool CanAttack = true;
        [SerializeField] public bool CanJump = true;
        [SerializeField] public bool CanInteract = true;

        [Header("===== AUDIO =====")]
        [SerializeField] public AudioClip JumpSound;
        [SerializeField] public AudioClip LandSound;
        [SerializeField] public AudioClip AttackSound;
        [SerializeField] public AudioClip HurtSound;
        [SerializeField] public AudioClip DeathSound;

        [Header("===== RUNTIME DATA =====")]
        [SerializeField] private float _currentHealth;
        [SerializeField] private bool _isDead;
        [SerializeField] private float _lastDamageTime;

        public float CurrentHealth => _currentHealth;
        public bool IsDead => _isDead;
        public float HealthPercentage => _currentHealth / MaxHealth;
        public bool IsFullHealth => _currentHealth >= MaxHealth;
        public bool IsLowHealth => _currentHealth < MaxHealth * 0.25f;

        public float MaxDPS => AttackDamage * AttackSpeed;

        
        #region Health Management
        
        void OnEnable()
        {
            if (_currentHealth <= 0 && !_isDead)
            {
                _currentHealth = MaxHealth;
            }

            OnValidate();
        }

        public void InitializeHealth()
        {
            _currentHealth = MaxHealth;
            _isDead = false;
            _lastDamageTime = 0f;
        }

        public bool TakeDamage(float damage)
        {
            if (_isDead) return false;

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            _lastDamageTime = Time.time;

            if (_currentHealth <= 0)
            {
                _isDead = true;
                return true;
            }
            
            return false;
        }

        public void Heal(float amount)
        {
            if (_isDead) return;
            _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
        }

        public void UpdateHealthRegen()
        {
            if (_isDead || IsFullHealth) return;
            
            if (Time.time - _lastDamageTime >= HealthRegenDelay)
                Heal(HealthRegenRate * Time.deltaTime);
        }

        #endregion

        // Para evitar valores indeseados        
        void OnValidate()
        {
            MaxHealth = Mathf.Max(1f, MaxHealth);
            AttackDamage = CanAttack ? Mathf.Max(1f, AttackDamage) : 0f;
            AttackSpeed = CanAttack ? Mathf.Max(0.1f, AttackSpeed) : 0f;
            if (!CanAttack) AttackRange = 0f;
        }

    }
}