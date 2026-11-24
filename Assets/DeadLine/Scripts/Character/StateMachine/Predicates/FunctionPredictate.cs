using System;

namespace DeadLine
{
    /// <summary>
    /// Predicado que evalúa una función booleana proporcionada
    /// </summary>
    public class FunctionPredicate : IPredicate
    {
        readonly Func<bool> function;
        readonly string description;

        public FunctionPredicate(Func<bool> function, string description = null)
        {
            this.function = function;
            this.description = description;
        }

        
        /// <summary>
        /// Evalúa la función booleana proporcionada
        /// </summary>
        public bool Evaluate()
        {
            return function.Invoke();
        }

        public override string ToString()
        {
            return description;
        }
    }
}
