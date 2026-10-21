using UnityEngine;
using UnityEngine.AI;
using Platformer;

[RequireComponent(typeof(NavMeshAgent),typeof(Animator))]
public class Enemy : MonoBehaviour
{
    public enum Phase { Idle, Chase, WaitForAttack, Windup, Swing, Recovery, HitReaction, Block, GuardBroken, Opening, Investigate, Return, Dead }
    public float maxHealth = 60;
    public float currentHealth;
    public HealthBar healthBar;
    [SerializeField] GameObject hitVFX;
    [SerializeField] GameObject ragdoll;
    [Header("Attack")]
    [SerializeField, Min(0)] float attackCD = 3;
    [SerializeField, Min(.1f)] float attackRange = 1.6f;
    [SerializeField, Min(.01f)] float windupDuration = .4f;
    [SerializeField, Min(.05f)] float swingDuration = .45f;
    [SerializeField, Min(.01f)] float recoveryDuration = .6f;
    [SerializeField, Range(0,1)] float hitStart = .2f;
    [SerializeField, Range(0,1)] float hitEnd = .7f;
    [Header("Perception and navigation")]
    [SerializeField, Min(.1f)] float aggroRange = 6;
    [SerializeField, Min(.1f)] float loseTargetRange = 9;
    [SerializeField, Min(0)] float lostSightDelay = 1;
    [SerializeField, Min(0)] float searchDuration = 2;
    [SerializeField, Min(1)] float navigationTimeout = 12;
    [SerializeField, Min(.1f)] float waitingRadius = 2.2f;
    [Header("Type and defense")]
    [SerializeField] bool heavy;
    [SerializeField] bool canBlock;
    public CombatGuard guard = new CombatGuard();
    [SerializeField, Min(.01f)] float reactionDuration = .25f;
    [SerializeField, Min(0)] float pushDistance = .3f;
    [SerializeField, Min(0)] float blockReactionDelay = .15f;
    [SerializeField, Min(.01f)] float blockHoldDuration = .6f;
    [SerializeField, Min(.01f)] float blockOpeningDuration = .35f;
    NavMeshAgent agent;
    Animator animator;
    CombatAnimation presentation;
    EnemyDamageDealer[] dealers;
    HealthSystem player;
    Vector3 home, lastSeen, push;
    float deadline, started, nextAttack, lastSight, investigateStart, searchUntil = -1;
    float blockAt = float.PositiveInfinity, openingUntil, nextPath;
    PlayerStateMachine blockSource;
    bool detected, window;
    public Phase State { get; private set; }
    public bool IsDead => State == Phase.Dead;
    public bool IsSwinging => State == Phase.Swing;
    bool Navigable => agent != null && agent.enabled && agent.isOnNavMesh;
    public bool ReadyToAttack => player != null && !player.IsDead && !IsDead &&
        (State == Phase.Chase || State == Phase.WaitForAttack) && Time.time >= nextAttack &&
        Time.time >= openingUntil && Vector3.Distance(transform.position,player.transform.position) <= attackRange && Visible();
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>(); animator = GetComponent<Animator>();
        presentation = new CombatAnimation(animator); dealers = GetComponentsInChildren<EnemyDamageDealer>(true);
        home = transform.position; currentHealth = Mathf.Max(1,maxHealth); guard.Initialize();
    }
    void Start() { FindPlayer(); if (healthBar != null) healthBar.SetMaxHealth(maxHealth); }
    void OnEnable() { PlayerStateMachine.AttackStarted += ObserveAttack; }
    void OnDisable()
    {
        PlayerStateMachine.AttackStarted -= ObserveAttack; EnemyAttackCoordinator.Release(this);
        CloseWindow(); blockAt = float.PositiveInfinity; blockSource = null;
        if (presentation != null) presentation.Stop();
        if (agent != null) agent.updateRotation = true;
    }
    void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player"); player = go != null ? go.GetComponent<HealthSystem>() : null;
    }
    bool Visible()
    {
        return player != null && !player.IsDead && Vector3.Distance(transform.position,player.transform.position) <= (detected ? Mathf.Max(aggroRange,loseTargetRange) : aggroRange) &&
            CombatVisibility.Clear(transform.position+Vector3.up,player.transform.position+Vector3.up,transform,player.transform);
    }
    void Update()
    {
        if (IsDead) return;
        guard.Tick(Time.deltaTime,Time.time,State == Phase.Block || State == Phase.GuardBroken);
        animator.SetFloat("speed",Navigable && agent.speed > 0 ? agent.velocity.magnitude/agent.speed : 0);
        if (player == null) FindPlayer();
        if (player == null || player.IsDead)
        {
            detected = false;
            if (State != Phase.Return && State != Phase.Idle) SetPhase(Phase.Return,navigationTimeout);
        }
        bool visible = Visible();
        if (visible)
        {
            detected = true; lastSeen = player.transform.position; lastSight = Time.time;
            if (State == Phase.Idle || State == Phase.Return || State == Phase.Investigate) SetPhase(Phase.Chase);
        }
        if (Time.time >= blockAt)
        {
            blockAt = float.PositiveInfinity;
            if (blockSource != null && player != null && blockSource.gameObject == player.gameObject && visible && CanStartBlock()) SetPhase(Phase.Block,blockHoldDuration);
            blockSource = null;
        }
        switch (State)
        {
            case Phase.Idle: StopAgent(); break;
            case Phase.Chase:
            case Phase.WaitForAttack:
                if (!visible && Time.time-lastSight >= lostSightDelay) { StartInvestigation(); break; }
                if (ReadyToAttack)
                {
                    StopAgent(); State = Phase.WaitForAttack;
                    if (EnemyAttackCoordinator.RequestAttack(this,player.transform)) SetPhase(Phase.Windup,windupDuration);
                }
                else
                {
                    EnemyAttackCoordinator.Release(this);
                    Vector3 destination = visible ? player.transform.position : lastSeen;
                    if (visible && Vector3.Distance(transform.position,destination) < waitingRadius && Time.time < nextAttack)
                    {
                        float angle = (GetInstanceID()%12)*Mathf.PI/6;
                        destination += new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*waitingRadius;
                    }
                    Navigate(destination);
                }
                break;
            case Phase.Windup:
                FacePlayer();
                if (Time.time >= deadline) SetPhase(Phase.Swing,swingDuration);
                break;
            case Phase.Swing:
                float progress = Mathf.InverseLerp(started,deadline,Time.time);
                bool next = progress >= hitStart && progress < hitEnd;
                if (next != window) { window = next; foreach (var d in dealers) { if (next) d.StartDealDamage(); else d.EndDealDamage(); } }
                if (Time.time >= deadline) SetPhase(Phase.Recovery,recoveryDuration);
                break;
            case Phase.Recovery:
                if (Time.time >= deadline) { EnemyAttackCoordinator.Release(this); nextAttack = Time.time+attackCD; ResumePursuit(visible); }
                break;
            case Phase.Block:
                FacePlayer();
                if (Time.time >= deadline || player == null || player.IsDead) { openingUntil = Time.time+blockOpeningDuration; SetPhase(Phase.Opening,blockOpeningDuration); }
                break;
            case Phase.Opening:
                if (Time.time >= deadline) ResumePursuit(visible);
                break;
            case Phase.HitReaction:
                ApplyPush();
                if (Time.time >= deadline) { if (Time.time < openingUntil) SetPhase(Phase.Opening,openingUntil-Time.time); else ResumePursuit(visible); }
                break;
            case Phase.GuardBroken:
                if (Time.time >= deadline) ResumePursuit(visible);
                break;
            case Phase.Investigate:
                if (searchUntil < 0)
                {
                    Navigate(lastSeen);
                    if (Reached() || Time.time-investigateStart > navigationTimeout) { StopAgent(); searchUntil = Time.time+searchDuration; }
                }
                else if (Time.time >= searchUntil) { detected = false; SetPhase(Phase.Return,navigationTimeout); }
                break;
            case Phase.Return:
                Navigate(home);
                if (Reached() || Time.time >= deadline) SetPhase(Phase.Idle);
                break;
        }
    }
    void ResumePursuit(bool visible) { if (visible) SetPhase(Phase.Chase); else StartInvestigation(); }
    void StartInvestigation()
    {
        EnemyAttackCoordinator.Release(this); investigateStart = Time.time; searchUntil = -1;
        SetPhase(Phase.Investigate);
    }
    bool CanStartBlock() => canBlock && guard.CanBlock && Time.time >= openingUntil &&
        (State == Phase.Chase || State == Phase.WaitForAttack);
    void ObserveAttack(PlayerStateMachine source)
    {
        if (!CanStartBlock() || player == null || source.gameObject != player.gameObject || !Visible() ||
            Vector3.Distance(transform.position,source.transform.position) > attackRange+1) return;
        if (float.IsPositiveInfinity(blockAt)) { blockAt = Time.time+blockReactionDelay; blockSource = source; }
    }
    void SetPhase(Phase next,float duration = 0)
    {
        Phase old = State;
        if (old == Phase.Block) guard.Delay(Time.time);
        if (old == Phase.Swing) CloseWindow();
        bool keepSlot = (old == Phase.Windup && next == Phase.Swing) || (old == Phase.Swing && next == Phase.Recovery) || next == Phase.Windup;
        if (!keepSlot) EnemyAttackCoordinator.Release(this);
        State = next; started = Time.time; deadline = Time.time+Mathf.Max(.01f,duration);
        blockAt = float.PositiveInfinity; blockSource = null;
        bool fixedAction = next == Phase.Windup || next == Phase.Swing || next == Phase.Recovery || next == Phase.Block || next == Phase.GuardBroken || next == Phase.HitReaction || next == Phase.Opening;
        if (fixedAction) StopAgent();
        agent.updateRotation = !fixedAction;
        presentation.Stop();
        switch(next)
        {
            case Phase.Windup: FacePlayer(); PlayAction("Windup",duration); break;
            case Phase.Swing: PlayAction("Attack",duration); break;
            case Phase.Block: presentation.Play("Block",1); break;
            case Phase.GuardBroken: PlayAction("GuardBreak",duration); break;
            case Phase.HitReaction: PlayAction("Hit",duration); break;
        }
    }
    void PlayAction(string state,float duration)
    {
        float length = 0;
        foreach (var clip in animator.runtimeAnimatorController.animationClips) if (clip.name == "V2_"+state || clip.name == "Enemy_"+state) { length = clip.length; break; }
        // Windup uses the first 20% of the clip; the swing continues at 20%
        // rather than replaying its preparation after the telegraph.
        float offset = state == "Attack" ? .2f : 0;
        if (state == "Windup") length *= .2f;
        else if (state == "Attack") length *= .8f;
        presentation.Play(state,duration,length,offset);
    }
    void FacePlayer()
    {
        if (player == null) return;
        Vector3 direction = Vector3.ProjectOnPlane(player.transform.position-transform.position,Vector3.up);
        if (direction.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(direction);
    }
    void StopAgent() { if (Navigable) { agent.isStopped = true; agent.ResetPath(); } }
    void Navigate(Vector3 destination)
    {
        if (!Navigable) return;
        agent.isStopped = false;
        if (Time.time < nextPath) return;
        nextPath = Time.time+.2f;
        if (NavMesh.SamplePosition(destination,out var sample,2,agent.areaMask)) agent.SetDestination(sample.position);
    }
    bool Reached() => Navigable && !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance+.2f);
    void ApplyPush()
    {
        if (!Navigable || heavy) return;
        float duration = Mathf.Max(.01f,reactionDuration);
        float t0 = Mathf.Clamp01((Time.time-Time.deltaTime-started)/duration), t1 = Mathf.Clamp01((Time.time-started)/duration);
        Vector3 delta = push*pushDistance*((2*t1-t1*t1)-(2*t0-t0*t0));
        if (!agent.Raycast(transform.position+delta,out _)) agent.Move(delta);
    }
    public void TakeDamage(float damage) { ReceiveDamage(damage,transform.position); }
    public HitResult ReceiveDamage(float damage,Vector3 source)
    {
        if (IsDead || !CombatVisibility.Valid(damage)) return HitResult.Ignored;
        bool blocked = State == Phase.Block && guard.Covers(transform,source);
        bool broken = false;
        if (blocked) { broken = guard.Spend(damage,Time.time); damage *= 1-Mathf.Clamp01(guard.reduction); }
        currentHealth = Mathf.Clamp(currentHealth-damage,0,maxHealth);
        if (healthBar != null) healthBar.SetHealth(currentHealth);
        if (currentHealth <= 0)
        {
            SetPhase(Phase.Dead); CloseWindow(); StopAgent();
            if (ragdoll != null) Instantiate(ragdoll,transform.position,transform.rotation);
            Destroy(gameObject); return HitResult.Killed;
        }
        if (broken) { openingUntil = 0; SetPhase(Phase.GuardBroken,guard.breakDuration); }
        else if (!blocked && State != Phase.Swing && State != Phase.HitReaction && State != Phase.GuardBroken)
        {
            push = Vector3.ProjectOnPlane(transform.position-source,Vector3.up).normalized;
            nextAttack = Time.time+recoveryDuration; SetPhase(Phase.HitReaction,reactionDuration);
        }
        return blocked ? broken ? HitResult.GuardBroken : HitResult.Blocked : HitResult.Hit;
    }
    void CloseWindow() { window = false; if (dealers != null) foreach(var d in dealers) d.EndDealDamage(); }
    // Legacy animation events cannot reopen/close a V2 window outside its clock.
    public void StartDealDamage() { }
    public void EndDealDamage() { }
    public void HitVFX(Vector3 point) { if (hitVFX != null) Destroy(Instantiate(hitVFX,point,Quaternion.identity),3); }
    public void HitVFX(Vector3 point,HitResult result) => CombatFeedback.Show(hitVFX,point,result);
    void OnDrawGizmosSelected() { Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position,attackRange); Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position,aggroRange); }
}
