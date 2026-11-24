using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
namespace DeadLine
{
    public class StateMachine
    {
        StateNode current;
        
        Dictionary<Type, StateNode> nodes = new();
        HashSet<ITransition> anyTransitions = new(); // Transiciones de cualquier estado a una especifico


        public void Update()
        {
            ITransition transition = GetTransition();
            
            if (transition != null)
            {
                ChangeState(transition.TargetState, transition);
            }

            current.State?.Update();
        }

        public void FixedUpdate()
        {
            current.State?.FixedUpdate();
        }

        public void SetState(IState state)
        {
            current = nodes[state.GetType()];
            current.State?.OnEnter();
        }

        public void ChangeState(IState state, ITransition transition)
        {
            if(state == current.State) return;
    
            IState previousState = current.State;
            IState nextState = nodes[state.GetType()].State;

            previousState?.OnExit();
            nextState?.OnEnter();

            current = nodes[state.GetType()];
        }

        private ITransition GetTransition()
        {
            foreach(ITransition transition in anyTransitions)
            {
                if(transition.TargetState == current.State)
                    continue;

                if(transition.Condition.Evaluate())
                    return transition;
            }

            foreach(ITransition transition in current.Transitions)
            {    
                if(transition.Condition.Evaluate())
                    return transition;
            }

            return null;
        }

        public void AddTransition(IState from, IState to, IPredicate condition)
        {
            GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
        }

        public void AddAnyTransition(IState to, IPredicate condition)
        {
            anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition));
        }

        public void AddAnyTransitionExcept(IState to, IState[] except, IPredicate condition)
        {
            HashSet<IState> exceptions = new HashSet<IState>(except);

            foreach(StateNode node in nodes.Values)
            {
                if (exceptions.Contains(node.State))
                    continue;
                
                AddTransition(node.State, to, condition);
            }
        }

        private StateNode GetOrAddNode(IState state)
        {
            StateNode node = nodes.GetValueOrDefault(state.GetType());

            if(node == null)
            {
                node = new StateNode(state);
                nodes.Add(state.GetType(), node);
            }

            return node;
        }

        public IState GetState()
        {
            return current.State;
        }

        
    }

    /// <summary>
    /// Nodo que representa un estado y sus transiciones
    /// </summary>
    internal class StateNode
    {
        public readonly IState State;
        public readonly HashSet<ITransition> Transitions;

        public StateNode(IState state)
        {
            State = state;
            Transitions = new HashSet<ITransition>();
        }

        public void AddTransition(ITransition transition)
        {
            Transitions.Add(transition);
        }

        public void AddTransition(IState to, IPredicate condition)
        {
            Transitions.Add(new Transition(to, condition));
        }
        
    }
}
