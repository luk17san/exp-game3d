using System;

public abstract class PlayerBaseState
{
    private bool _isRootState;
    private bool _active;
    private int _revision;
    private PlayerBaseState _currentSubState;
    private PlayerBaseState _currentSuperState;
    protected bool IsRootState { set => _isRootState = value; }
    protected PlayerStateMachine Ctx { get; }
    protected PlayerStateFactory Factory { get; }

    protected PlayerBaseState(PlayerStateMachine context, PlayerStateFactory factory)
    {
        Ctx = context;
        Factory = factory;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();
    public abstract void CheckSwitchState();
    public abstract void InitalizeSubState();

    // Tylko te metody obsługują cykl życia. Nie wywoływać EnterState ręcznie.
    public void EnterStates()
    {
        if (_active) throw new InvalidOperationException("State already active.");
        _active = true;
        _revision++;
        EnterState();
    }

    public void ExitStates()
    {
        if (!_active) return;
        _active = false;
        _revision++;
        var child = _currentSubState;
        _currentSubState = null;
        if (child != null)
        {
            child.ExitStates();
            child._currentSuperState = null;
        }
        ExitState();
    }

    public void UpdateStates()
    {
        if (!_active) return;
        int revision = _revision;
        UpdateState();
        if (_active && revision == _revision)
            _currentSubState?.UpdateStates();
    }

    protected void SwitchState(PlayerBaseState next)
    {
        if (next == null) throw new ArgumentNullException(nameof(next));
        if (ReferenceEquals(next, this)) return;
        if (next._active) throw new InvalidOperationException("Target state already active.");
        if (_isRootState)
        {
            ExitStates();
            Ctx.CurrentState = next;
            next.EnterStates();
        }
        else
        {
            if (_currentSuperState == null)
                throw new InvalidOperationException("Substate has no parent.");
            _currentSuperState.SetSubState(next);
        }
    }

    protected void SetSubState(PlayerBaseState next)
    {
        if (next == null) throw new ArgumentNullException(nameof(next));
        if (ReferenceEquals(next, _currentSubState)) return;
        if (next._active) throw new InvalidOperationException("Substate already active.");
        var old = _currentSubState;
        if (old != null)
        {
            old.ExitStates();
            old._currentSuperState = null;
        }
        _currentSubState = next;
        next._currentSuperState = this;
        if (_active) next.EnterStates();
    }
}
