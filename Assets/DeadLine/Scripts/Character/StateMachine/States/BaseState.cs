namespace DeadLine
{
    public abstract class BaseState : IState 
    {
        protected readonly Character _character;   

        public BaseState(Character character)
        {
            this._character = character;
        }
        
        public virtual void OnEnter()
        {
            EnterState();
        }
        
        public virtual void Update()
        {
            HandleInput();
            HandleRotation();
            UpdateStateLogic();
        }
        
        public virtual void FixedUpdate()
        {
            ApplyPhysics();
            ApplyMovement();
        }
        
        public virtual void OnExit()
        {
            ExitState();
        }

       
        /// <summary>
        /// Inicialización al entrar al estado
        /// </summary>
        protected virtual void EnterState() { }
        
        /// <summary>
        /// Procesar input del jugador cada frame
        /// </summary>
        protected virtual void HandleInput() { }
        
        /// <summary>
        /// Rotar el personaje (tiene implementación por defecto)
        /// </summary>
        protected virtual void HandleRotation() 
        {
            _character.Movement.HandleRotation();
        }
        
        /// <summary>
        /// Lógica específica del estado que se ejecuta cada frame
        /// </summary>
        protected virtual void UpdateStateLogic() { }
        
        /// <summary>
        /// Aplicar física (gravedad, fuerzas) cada FixedUpdate
        /// </summary>
        protected virtual void ApplyPhysics() { }
        
        /// <summary>
        /// Aplicar movimiento horizontal/vertical cada FixedUpdate
        /// </summary>
        protected virtual void ApplyMovement() { }
        
        /// <summary>
        /// Limpieza al salir del estado
        /// </summary>
        protected virtual void ExitState() { }
    }
}