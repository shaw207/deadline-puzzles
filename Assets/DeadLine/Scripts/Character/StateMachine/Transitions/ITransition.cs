namespace DeadLine
{
    public interface ITransition
    {
        IState TargetState {get;}
        IPredicate Condition {get;}
    }
}
