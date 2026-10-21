public class PlayerReactionState : PlayerBaseState
{
    public bool Broken { get; set; }
    public PlayerReactionState(PlayerStateMachine c, PlayerStateFactory f) : base(c,f) { IsRootState = true; }
    public override void EnterState() { Ctx.StopHorizontalMovement(); Ctx.PlayAction(Broken ? "GuardBreak" : "Hit",Broken ? Ctx.guard.breakDuration : Ctx.HitReactionDuration); }
    public override void UpdateState() { Ctx.GroundGravity(); CheckSwitchState(); }
    public override void CheckSwitchState()
    {
        // Keep the deadline while falling; no locomotion until the reaction ends.
        if (!Ctx.IsReacting) SwitchState(Ctx.CharacterController.isGrounded ? Factory.Grounded() : Factory.Fall());
    }
    public override void ExitState() { Ctx.EndAction(); }
    public override void InitalizeSubState() { }
}
