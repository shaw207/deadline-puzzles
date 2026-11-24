using System.Collections.Generic;
using UnityEngine;

namespace DeadLine
{
    public interface IState
    {
        public void OnEnter();
        public void Update();
        public void FixedUpdate();
        public void OnExit();
    }
}
