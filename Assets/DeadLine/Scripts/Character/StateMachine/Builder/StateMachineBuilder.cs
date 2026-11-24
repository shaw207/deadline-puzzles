using UnityEngine;

namespace DeadLine
{
    /// <summary>
    /// Clase base abstracta para construir máquinas de estado.
    /// Proporciona métodos helper comunes para añadir transiciones.
    /// </summary>
    public abstract class StateMachineBuilder
    {
        protected readonly Character _character;
        protected readonly StateMachine _stateMachine;

        protected StateMachineBuilder(Character character)
        {
            _character = character;
            _stateMachine = new StateMachine();
        }

        /// <summary>
        /// Construye y devuelve la máquina de estado configurada.
        /// Las clases hijas deben implementar la lógica específica de creación de estados y transiciones.
        /// </summary>
        public abstract StateMachine Build();

        #region Transition Helpers

        /// <summary>
        /// Añade una transición simple entre dos estados
        /// </summary>
        protected void AddTransition(IState from, IState to, IPredicate condition)
        {
            _stateMachine.AddTransition(from, to, condition);
        }
        
        /// <summary>
        /// Añade una transición desde múltiples estados hacia un solo estado destino
        /// </summary>
        protected void AddTransition(IState[] fromStates, IState to, IPredicate condition)
        {
            foreach (IState state in fromStates)
            {
                AddTransition(state, to, condition);
            }
        }

        /// <summary>
        /// Añade una transición desde cualquier estado excepto los especificados
        /// </summary>
        protected void AddAnyTransitionExcept(IState to, IState[] except, IPredicate condition)
        {
            _stateMachine.AddAnyTransitionExcept(to, except, condition);
        }
        
        /// <summary>
        /// Añade una transición desde cualquier estado excepto uno
        /// </summary>
        protected void AddAnyTransitionExcept(IState to, IState except, IPredicate condition)
        {
            _stateMachine.AddAnyTransitionExcept(to, new[] {except}, condition);
        }
        
        /// <summary>
        /// Añade una transición desde cualquier estado
        /// </summary>
        protected void AddAnyTransition(IState to, IPredicate condition)
        {
            _stateMachine.AddAnyTransition(to, condition);
        }

        #endregion
    }
}
