using System.Collections.Generic;

enum PlayerStates
    {
    idle,
    walk,
    run,
    grounded,
    jump,
    fall
    }

enum AttackStates
{
    combat,
    meleeAttack,
    weaponAttack,
    meleeBlock,
    weaponBlock
}
public class PlayerStateFactory
{
    PlayerReactionState reaction;
    public PlayerBaseState HitReaction() => reaction;
    PlayerStateMachine _context;
    Dictionary<PlayerStates,PlayerBaseState> _states = new Dictionary<PlayerStates,PlayerBaseState>();
    Dictionary<AttackStates, PlayerBaseState> _statesAttack = new Dictionary<AttackStates, PlayerBaseState>();


    public PlayerStateFactory(PlayerStateMachine currentContext)
    {
        _context = currentContext;
        reaction = new PlayerReactionState(_context, this);
        _states[PlayerStates.idle] = new PlayerIdleState(_context,this);
        _states[PlayerStates.walk] = new PlayerWalkState(_context, this);
        _states[PlayerStates.run] = new PlayerRunState(_context, this);
        _states[PlayerStates.grounded] = new PlayerGroundedState(_context, this);
        _states[PlayerStates.jump] = new PlayerJumpState(_context, this);
        _states[PlayerStates.fall] = new PlayerFallState(_context, this);

        _statesAttack[AttackStates.combat] = new PlayerCombatState(_context, this);
        _statesAttack[AttackStates.meleeAttack] = new MeleeAttackState(_context, this);
        _statesAttack[AttackStates.weaponAttack] = new WeaponAttackState(_context, this);
        _statesAttack[AttackStates.meleeBlock] = new MeleeBlockState(_context, this);
        _statesAttack[AttackStates.weaponBlock] = new WeaponBlockState(_context, this);


    }

    public PlayerBaseState Idle() {
        return _states[PlayerStates.idle];
    }
    public PlayerBaseState Walk() {
        return _states[PlayerStates.walk];
    }
    public PlayerBaseState Run() {
        return _states[PlayerStates.run];
    }
    public PlayerBaseState Jump() {
        return _states[PlayerStates.jump];
    }
    public PlayerBaseState Grounded() {
        return _states[PlayerStates.grounded];
    }
    public PlayerBaseState Fall() {
        return _states[PlayerStates.fall];
    }


    public PlayerBaseState Combat() {
        return _statesAttack[AttackStates.combat];
    }
    public PlayerBaseState MeleeAttack() {
        return _statesAttack[AttackStates.meleeAttack];
    }
    public PlayerBaseState WeaponAttack() {
        return _statesAttack[AttackStates.weaponAttack];
    }
    public PlayerBaseState MeleeBlock()
    {
        return _statesAttack[(AttackStates.meleeBlock)];
    }
    public PlayerBaseState WeaponBlock()
    {
        return _statesAttack[(AttackStates.weaponBlock)];
    }


}

