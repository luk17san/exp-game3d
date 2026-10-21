public class PlayerCombatState : PlayerBaseState
{
    public PlayerCombatState(PlayerStateMachine c, PlayerStateFactory f) : base(c,f) { IsRootState = true; }
    public override void EnterState() { Ctx.ToggleWeapon(); }
    public override void UpdateState() => CheckSwitchState();
    public override void CheckSwitchState() => SwitchState(Ctx.CharacterController.isGrounded ? Factory.Grounded() : Factory.Fall());
    public override void ExitState() { }
    public override void InitalizeSubState() { }
}
