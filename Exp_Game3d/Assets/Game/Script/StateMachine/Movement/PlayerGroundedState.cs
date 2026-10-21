public class PlayerGroundedState : PlayerBaseState, IRootState
{
    public PlayerGroundedState(PlayerStateMachine c, PlayerStateFactory f) : base(c,f) { IsRootState = true; }
    public void HandleGravity() => Ctx.GroundGravity();
    public override void EnterState() { InitalizeSubState(); HandleGravity(); }
    public override void ExitState() { }
    public override void UpdateState() { HandleGravity(); CheckSwitchState(); }
    public override void InitalizeSubState() => SetSubState(!Ctx.IsMovementPressed ? Factory.Idle() : Ctx.IsRunPressed ? Factory.Run() : Factory.Walk());
    public override void CheckSwitchState()
    {
        if (!Ctx.CharacterController.isGrounded) SwitchState(Factory.Fall());
        else if (Ctx.ConsumeAttack()) SwitchState(Ctx.WeaponDrawed ? Factory.WeaponAttack() : Factory.MeleeAttack());
        else if (Ctx.CanBlock) SwitchState(Ctx.WeaponDrawed ? Factory.WeaponBlock() : Factory.MeleeBlock());
        else if (Ctx.IsJumpPressed && !Ctx.RequireNewJumpPress) SwitchState(Factory.Jump());
        else if (Ctx.ConsumeDraw()) Ctx.ToggleWeapon();
    }
}
