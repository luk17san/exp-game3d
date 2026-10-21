using UnityEngine;
using Platformer;
public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100;
    public float currentHealth;
    public HealthBar healthBar;
    [SerializeField] GameObject hitVFX;
    [SerializeField] GameObject ragdoll;
    Animator animator;
    PlayerStateMachine combat;
    public bool IsDead { get; private set; }
    void Awake()
    {
        animator = GetComponent<Animator>(); combat = GetComponent<PlayerStateMachine>();
        maxHealth = Mathf.Max(1,maxHealth); currentHealth = maxHealth;
    }
    void Start() { if (healthBar != null) healthBar.SetMaxHealth(maxHealth); }
    public void TakeDamage(float damage) { ReceiveDamage(damage,transform.position); }
    public HitResult ReceiveDamage(float damage,Vector3 source)
    {
        if (IsDead || !CombatVisibility.Valid(damage)) return HitResult.Ignored;
        bool blocked = false;
        if (combat != null && combat.isActiveAndEnabled) damage = combat.Defend(damage,source,out blocked);
        currentHealth = Mathf.Clamp(currentHealth-damage,0,maxHealth);
        if (healthBar != null) healthBar.SetHealth(currentHealth);
        if (currentHealth <= 0)
        {
            IsDead = true;
            if (combat != null) combat.enabled = false;
            var oldMovement = GetComponent<Movement>(); if (oldMovement != null) oldMovement.enabled = false;
            if (ragdoll != null) Instantiate(ragdoll,transform.position,transform.rotation);
            Destroy(gameObject); return HitResult.Killed;
        }
        if (!blocked)
        {
            if (combat != null && combat.isActiveAndEnabled) combat.ReceiveHit(source);
            else if (animator != null) animator.SetTrigger("damage");
        }
        return blocked ? combat.IsGuardBroken ? HitResult.GuardBroken : HitResult.Blocked : HitResult.Hit;
    }
    public void HitVFX(Vector3 point) { if (hitVFX != null) Destroy(Instantiate(hitVFX,point,Quaternion.identity),3); }
    public void HitVFX(Vector3 point,HitResult result) => CombatFeedback.Show(hitVFX,point,result);
}
