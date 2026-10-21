using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(.01f)] float walkSpeed = 1;
    [SerializeField, Min(.01f)] float runMultiplier = 4;
    [SerializeField, Min(0)] float rotationSpeed = 15;
    [SerializeField] float gravity = -9.8f;
    [SerializeField, Min(.01f)] float maxJumpHeight = 4;
    [SerializeField, Min(.01f)] float maxJumpTime = .75f;
    [Header("Combat")]
    public CombatStrike[] meleeCombo = { new CombatStrike("Melee1"), new CombatStrike("Melee2"), new CombatStrike("Melee3") };
    public CombatStrike[] weaponCombo = { new CombatStrike("Weapon1"), new CombatStrike("Weapon2"), new CombatStrike("Weapon3") };
    [SerializeField, Min(0)] float comboWindow = .3f;
    [SerializeField, Min(0)] float meleeAimRange = 1.8f;
    [SerializeField, Min(0)] float weaponAimRange = 2.5f;
    [SerializeField, Min(0)] float fistDamage = 10;
    [SerializeField, Min(.01f)] float fistRadius = .12f;
    [Header("Guard and hit reaction")]
    public CombatGuard guard = new CombatGuard();
    [SerializeField, Range(0, 1)] float meleeBlockReduction = .5f;
    [SerializeField, Range(0, 1)] float weaponBlockReduction = 1;
    [SerializeField, Range(0, 1)] float blockMoveMultiplier = .4f;
    [SerializeField, Min(.01f)] float hitReactionDuration = .25f;
    [SerializeField, Min(0)] float hitPushDistance = .3f;
    public static event Action<PlayerStateMachine> AttackStarted;
    readonly Dictionary<int, float> jumpVelocities = new Dictionary<int, float>();
    readonly Dictionary<int, float> jumpGravities = new Dictionary<int, float>();
    readonly HashSet<Enemy> hitTargets = new HashSet<Enemy>();
    readonly Dictionary<Enemy, float> hitDistances = new Dictionary<Enemy, float>();
    readonly List<DamageDealer> fists = new List<DamageDealer>();
    CharacterController controller;
    Animator animator;
    PlayerInput input;
    PlayerStateFactory states;
    CombatAnimation presentation;
    EquipmentSystem equipment;
    Camera view;
    Vector2 moveInput;
    Vector3 velocity, pushDirection;
    float verticalCurrent;
    bool run, jump, attack, block, draw, attackEdge, drawEdge, buffered;
    bool needsBlockRelease, initialized, comboWeapon;
    float comboDeadline = float.NegativeInfinity, reactionEnd, reactionStart, reactionDistance;
    Enemy lastTarget, previousTarget;
    public PlayerBaseState CurrentState { get; set; }
    public Animator Animator => animator;
    public CharacterController CharacterController => controller;
    public Coroutine CurrentJumpResetRoutine { get; set; }
    public Dictionary<int,float> InitialJumpVelocities => jumpVelocities;
    public Dictionary<int,float> JumpGravities => jumpGravities;
    public int AttackCount { get; set; }
    public int JumpCount { get; set; }
    public bool RequireNewJumpPress { get; set; }
    public bool RequireNewAttackPress { get; set; }
    public bool RequireNewWeaponDrawPress { get; set; }
    public bool IsJumping { get; set; }
    public bool ActiveAttack { get; set; }
    public bool WeaponDrawed { get; set; }
    public bool IsMovementPressed => moveInput.sqrMagnitude > .0001f;
    public bool IsRunPressed => run;
    public bool IsJumpPressed => jump;
    public bool IsAttack1Pressed => attack;
    public bool IsBlockPressed => block;
    public bool IsWeaponDrawPressed => draw;
    public bool IsBlocking => isActiveAndEnabled && CurrentState is PlayerBlockState && block && controller.isGrounded;
    public bool CanBlock => block && !needsBlockRelease && guard.CanBlock && Time.time >= reactionEnd;
    public bool IsReacting => Time.time < reactionEnd;
    public float HitReactionDuration => hitReactionDuration;
    public bool IsGuardBroken => Time.time < reactionEnd && CurrentState is PlayerReactionState && ((PlayerReactionState)CurrentState).Broken;
    public float Gravity => gravity;
    public float RunMultiplier => runMultiplier * walkSpeed;
    public float WalkSpeed => walkSpeed;
    public Vector2 CurrentMovementInput => moveInput;
    public float CurrentMovementY { get => verticalCurrent; set => verticalCurrent = value; }
    public float AppliedMovementY { get => velocity.y; set => velocity.y = value; }
    public float AppliedMovementX { get => velocity.x; set => velocity.x = value; }
    public float AppliedMovementZ { get => velocity.z; set => velocity.z = value; }
    public int IsWalkingHash => UnityEngine.Animator.StringToHash("isWalking");
    public int IsRunningHash => UnityEngine.Animator.StringToHash("isRunning");
    public int IsJumpingHash => UnityEngine.Animator.StringToHash("isJumping");
    public int JumpCountHash => UnityEngine.Animator.StringToHash("jumpCount");
    public int IsFallingHash => UnityEngine.Animator.StringToHash("isFalling");
    public int AttackCountHash => UnityEngine.Animator.StringToHash("attackCount");
    public int IsWeaponAttackHash => UnityEngine.Animator.StringToHash("weaponAttack");
    public int IsMeleeAttackHash => UnityEngine.Animator.StringToHash("meleeAttack");
    public int IsWeaponDrawHash => UnityEngine.Animator.StringToHash("drawWeapon");
    public int IsBlockingHash => UnityEngine.Animator.StringToHash("isBlock");
    public Enemy LastTarget => ValidTarget(lastTarget) ? lastTarget : null;
    public float StaminaNormalized => guard.Current / Mathf.Max(1, guard.maxStamina);

    void Awake()
    {
        controller = GetComponent<CharacterController>(); animator = GetComponent<Animator>();
        controller.minMoveDistance = 0; // Keep grounding valid even for small high-frame-rate steps.
        equipment = GetComponent<EquipmentSystem>(); input = new PlayerInput();
        presentation = new CombatAnimation(animator); guard.Initialize();
        input.CharacterControls.Move.performed += c => moveInput = Vector2.ClampMagnitude(c.ReadValue<Vector2>(), 1);
        input.CharacterControls.Move.canceled += c => moveInput = Vector2.zero;
        input.CharacterControls.Run.started += c => run = true;
        input.CharacterControls.Run.canceled += c => run = false;
        input.CharacterControls.Jump.started += c => SetJumpInput(true);
        input.CharacterControls.Jump.canceled += c => SetJumpInput(false);
        input.CharacterControls.Attack1.started += c => { attack = true; attackEdge = true; };
        input.CharacterControls.Attack1.canceled += c => attack = false;
        input.CharacterControls.Block.started += c => block = true;
        input.CharacterControls.Block.canceled += c => { block = false; needsBlockRelease = false; guard.Delay(Time.time); };
        input.CharacterControls.DrawWeapon.started += c => { draw = true; drawEdge = true; };
        input.CharacterControls.DrawWeapon.canceled += c => { draw = false; RequireNewWeaponDrawPress = false; };
        float apex = maxJumpTime / 2;
        for (int i = 1; i <= 3; i++)
        {
            float t = apex * (1 + (i-1)*.25f), h = maxJumpHeight + (i-1)*2;
            jumpVelocities[i] = 2*h/t; jumpGravities[i] = -2*h/(t*t);
        }
        jumpGravities[0] = jumpGravities[1];
        states = new PlayerStateFactory(this); CurrentState = states.Grounded(); initialized = true;
    }
    void Start()
    {
        AddFist(HumanBodyBones.LeftHand); AddFist(HumanBodyBones.RightHand);
    }
    void AddFist(HumanBodyBones bone)
    {
        if (!animator.isHuman) return;
        var hand = animator.GetBoneTransform(bone); if (hand == null) return;
        var dealer = hand.gameObject.AddComponent<DamageDealer>();
        dealer.ConfigureFist(this, fistDamage, fistRadius); fists.Add(dealer);
    }
    void OnEnable() { if (!initialized) return; CurrentState.EnterStates(); input.CharacterControls.Enable(); }
    void OnDisable()
    {
        if (!initialized) return;
        input.CharacterControls.Disable(); CurrentState.ExitStates(); StopAllCoroutines();
        CloseDamageWindows(); presentation.Stop(); ResetCombo();
        CurrentJumpResetRoutine = null; JumpCount = 0; IsJumping = false;
        attackEdge = drawEdge = buffered = false; run = jump = attack = block = draw = false;
        RequireNewJumpPress = RequireNewAttackPress = RequireNewWeaponDrawPress = false;
        moveInput = Vector2.zero; velocity = Vector3.zero; verticalCurrent = 0; ActiveAttack = false;
        // Stamina and reaction deadlines survive a temporary disable.
        guard.Delay(Time.time); CurrentState = states.Grounded();
    }
    void OnDestroy() { input?.Dispose(); }
    void SetJumpInput(bool pressed)
    {
        jump = pressed;
        if (!pressed) RequireNewJumpPress = false;
        else if (ActiveAttack || IsReacting || IsBlocking) RequireNewJumpPress = true;
    }
    void Update()
    {
        guard.Tick(Time.deltaTime, Time.time, IsBlocking || IsGuardBroken);
        if (!ActiveAttack && AttackCount > 0 && Time.time > comboDeadline) ResetCombo();
        if (IsReacting && !(CurrentState is PlayerReactionState)) ChangeState(states.HitReaction());
        CurrentState.UpdateStates();
        Vector3 world = CameraSpace(velocity);
        if (IsReacting && !IsGuardBroken)
        {
            float duration = Mathf.Max(.01f, hitReactionDuration);
            float t0 = Mathf.Clamp01((Time.time - Time.deltaTime - reactionStart) / duration);
            float t1 = Mathf.Clamp01((Time.time - reactionStart) / duration);
            // Integrated ease-out: total travel is exactly reactionDistance without obstacles.
            world += pushDirection * reactionDistance * ((2*t1-t1*t1)-(2*t0-t0*t0)) / Mathf.Max(.00001f, Time.deltaTime);
        }
        if (IsBlocking) FaceTarget();
        else if (!ActiveAttack && !IsReacting && world.sqrMagnitude > .0001f && IsMovementPressed) Face(world, false);
        controller.Move(world * Time.deltaTime);
        attackEdge = drawEdge = false;
    }
    public void ChangeState(PlayerBaseState next)
    {
        if (ReferenceEquals(next, CurrentState)) return;
        CurrentState.ExitStates(); CurrentState = next; next.EnterStates();
    }
    public Vector3 CameraSpace(Vector3 v)
    {
        if (view == null) view = Camera.main;
        if (view == null) return v;
        Vector3 f = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        if (f.sqrMagnitude < .0001f) f = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        return f*v.z + Vector3.Cross(Vector3.up, f)*v.x + Vector3.up*v.y;
    }
    void Face(Vector3 direction, bool instant)
    {
        direction.y = 0; if (direction.sqrMagnitude < .0001f) return;
        var target = Quaternion.LookRotation(direction);
        transform.rotation = instant ? target : Quaternion.Slerp(transform.rotation, target, 1-Mathf.Exp(-rotationSpeed*Time.deltaTime));
    }
    void FaceTarget() { if (LastTarget != null) Face(lastTarget.transform.position-transform.position, false); }
    public bool ConsumeAttack() { bool result = attackEdge; attackEdge = false; return result; }
    public bool ConsumeDraw() { bool result = drawEdge; drawEdge = false; return result; }
    public void StopHorizontalMovement()
    {
        velocity.x = velocity.z = 0;
        animator.SetBool(IsWalkingHash, false); animator.SetBool(IsRunningHash, false);
    }
    public void GroundGravity()
    {
        if (controller.isGrounded) verticalCurrent = -2;
        else verticalCurrent = Mathf.Max(-20, verticalCurrent + gravity*Time.deltaTime);
        velocity.y = verticalCurrent;
    }
    public CombatStrike Strike => (WeaponDrawed ? weaponCombo : meleeCombo)[Mathf.Clamp(AttackCount-1,0,2)];
    public void StartStrike(bool continuation)
    {
        if (!continuation)
            AttackCount = comboWeapon == WeaponDrawed && Time.time <= comboDeadline && AttackCount < 3 ? AttackCount+1 : 1;
        else AttackCount++;
        comboWeapon = WeaponDrawed; buffered = false; ActiveAttack = true;
        previousTarget = LastTarget; hitTargets.Clear(); hitDistances.Clear();
        StopHorizontalMovement(); CloseDamageWindows();
        Vector3 direction = CameraSpace(Vector3.forward);
        if (LastTarget != null && Vector3.Distance(transform.position,lastTarget.transform.position) <= (WeaponDrawed ? weaponAimRange : meleeAimRange))
            direction = lastTarget.transform.position - transform.position;
        Face(direction,true);
        animator.SetInteger(AttackCountHash, AttackCount);
        PlayAction(Strike.animationState, Strike.duration);
        AttackStarted?.Invoke(this);
    }
    public void BufferAttack(float remaining)
    {
        if (ConsumeAttack() && AttackCount < 3 && remaining <= Strike.buffer) buffered = true;
    }
    public bool FinishStrike()
    {
        CloseDamageWindows(); ResolveTarget();
        if (buffered && AttackCount < 3) return true;
        ActiveAttack = false; comboDeadline = Time.time + comboWindow;
        if (AttackCount >= 3) ResetCombo();
        return false;
    }
    public void ResetCombo() { AttackCount = 0; buffered = false; comboDeadline = float.NegativeInfinity; }
    public bool RegisterHit(Enemy enemy)
    {
        if (!ActiveAttack || !hitTargets.Add(enemy)) return false;
        hitDistances[enemy] = Vector3.ProjectOnPlane(enemy.transform.position-transform.position,Vector3.up).sqrMagnitude;
        return true;
    }
    void ResolveTarget()
    {
        if (ValidTarget(previousTarget) && hitTargets.Contains(previousTarget)) { lastTarget = previousTarget; return; }
        Enemy best = null; float distance = float.PositiveInfinity;
        foreach (var candidate in hitDistances)
            if (ValidTarget(candidate.Key) && (candidate.Value < distance ||
                (candidate.Value == distance && best != null && candidate.Key.GetInstanceID() < best.GetInstanceID())))
            { best = candidate.Key; distance = candidate.Value; }
        if (best != null) lastTarget = best; else if (!ValidTarget(lastTarget)) lastTarget = null;
    }
    static bool ValidTarget(Enemy e) => e != null && e.isActiveAndEnabled && !e.IsDead;
    public void SetDamageWindow(bool enabled)
    {
        if (equipment != null) equipment.SetDamageWindow(enabled && WeaponDrawed && ActiveAttack);
        foreach (var fist in fists) { if (enabled && !WeaponDrawed && ActiveAttack) fist.StartDealDamage(); else fist.EndDealDamage(); }
    }
    public void CloseDamageWindows() { SetDamageWindow(false); }
    public void PlayAction(string state, float duration)
    {
        float length = 0;
        if (animator.runtimeAnimatorController != null)
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == "V2_"+state || clip.name == "Player_"+state) { length = clip.length; break; }
        presentation.Play(state,duration,length);
    }
    public void EndAction() { presentation.Stop(); }
    public void TickBlock()
    {
        GroundGravity(); velocity.x = moveInput.x*walkSpeed*blockMoveMultiplier; velocity.z = moveInput.y*walkSpeed*blockMoveMultiplier;
    }
    public void EndBlock() { guard.Delay(Time.time); }
    public float Defend(float damage, Vector3 source, out bool blockedHit)
    {
        blockedHit = IsBlocking && guard.Covers(transform,source);
        if (!blockedHit) return damage;
        float result = damage*(1-(WeaponDrawed ? weaponBlockReduction : meleeBlockReduction));
        if (guard.Spend(damage,Time.time))
        {
            needsBlockRelease = block;
            BeginReaction(guard.breakDuration,0,source,true);
        }
        return result;
    }
    public void ReceiveHit(Vector3 source)
    {
        if (!ActiveAttack && !IsReacting) BeginReaction(hitReactionDuration,hitPushDistance,source,false);
    }
    void BeginReaction(float duration,float distance,Vector3 source,bool broken)
    {
        reactionStart = Time.time; reactionEnd = Time.time+Mathf.Max(.01f,duration); reactionDistance = distance;
        pushDirection = Vector3.ProjectOnPlane(transform.position-source,Vector3.up).normalized;
        ResetCombo(); var state = (PlayerReactionState)states.HitReaction(); state.Broken = broken; ChangeState(state);
    }
    public void ToggleWeapon()
    {
        ResetCombo(); WeaponDrawed = !WeaponDrawed;
        animator.SetBool(IsWeaponDrawHash,WeaponDrawed);
        if (equipment != null) { if (WeaponDrawed) equipment.DrawWeapon(); else equipment.SheathWeapon(); }
    }
}
