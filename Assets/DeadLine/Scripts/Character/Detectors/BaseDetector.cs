using UnityEngine;

namespace DeadLine
{
    public abstract class BaseDetector : MonoBehaviour
    {
        public Character _character;

        protected abstract void OnDrawGizmos();

        public Character Character
        {
            private get { return _character; }
            set { _character = value; }
        }
    }
}
