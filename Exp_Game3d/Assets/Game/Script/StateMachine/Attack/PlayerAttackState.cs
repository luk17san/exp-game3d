using UnityEngine;
public abstract class PlayerAttackState : PlayerBaseState
{
    float end, start;
    bool window;
    protected PlayerAttackState(PlayerStateMachine c, PlayerStateFactory f) : base(c,f) { IsRootState = true; }
    public override void EnterState() { Begin(false); }
    void Begin(bool continuation)
    {
        Ctx.StartStrike(continuation); start = Time.time; end = start+Mathf.Max(.05f,Ctx.Strike.duration); window = false;
    }
    public override void UpdateState()
    {
        Ctx.GroundGravity(); Ctx.BufferAttack(end-Time.time);
        float progress = Mathf.InverseLerp(start,end,Time.time);
        bool nextWindow = progress >= Ctx.Strike.hitStart && progress < Ctx.Strike.hitEnd;
        if (window != nextWindow) { window = nextWindow; Ctx.SetDamageWindow(window); }
        CheckSwitchState();
    }
    public override void CheckSwitchState()
    {
        if (!Ctx.CharacterController.isGrounded) { Ctx.ResetCombo(); SwitchState(Factory.Fall()); return; }
        if (Time.time < end) return;
        if (Ctx.FinishStrike()) Begin(true);
        else SwitchState(Factory.Grounded());
    }
    public override void ExitState() { Ctx.CloseDamageWindows(); Ctx.ActiveAttack = false; Ctx.EndAction(); }
    public override void InitalizeSubState() { }
}
