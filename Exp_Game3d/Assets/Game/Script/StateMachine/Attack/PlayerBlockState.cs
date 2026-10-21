public abstract class PlayerBlockState : PlayerBaseState
{
    protected PlayerBlockState(PlayerStateMachine c, PlayerStateFactory f) : base(c,f) { IsRootState = true; }
    public override void EnterState() { Ctx.StopHorizontalMovement(); Ctx.PlayAction("Block",1); }
    public override void UpdateState() { Ctx.TickBlock(); CheckSwitchState(); }
    public override void CheckSwitchState()
    {
        if (!Ctx.CharacterController.isGrounded) SwitchState(Factory.Fall());
        else if (!Ctx.IsBlockPressed) SwitchState(Factory.Grounded());
    }
    public override void ExitState() { Ctx.EndBlock(); Ctx.EndAction(); }
    public override void InitalizeSubState() { }
}
