using System.Collections.Generic;
using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField, Min(0)] float weaponLength = .8f;
    [SerializeField, Min(0)] float weaponDamage = 10;
    [SerializeField, Min(.01f)] float contactRadius = .08f;
    [SerializeField] LayerMask targetLayers = 1 << 9;
    PlayerStateMachine owner;
    bool active;
    Vector3 previousBase, previousTip;
    readonly HashSet<Enemy> hit = new HashSet<Enemy>();
    public void Bind(PlayerStateMachine player) { owner = player; }
    public void ConfigureFist(PlayerStateMachine player, float damage, float radius)
    { owner = player; weaponLength = 0; weaponDamage = damage; contactRadius = radius; }
    void Awake() { owner = GetComponentInParent<PlayerStateMachine>(); }
    void OnDisable() { active = false; hit.Clear(); }
    public void StartDealDamage()
    {
        if (active || owner == null || !owner.ActiveAttack) return;
        active = true; hit.Clear(); previousBase = transform.position; previousTip = Tip;
    }
    public void EndDealDamage() { active = false; }
    Vector3 Tip => transform.position - transform.up * weaponLength;
    void LateUpdate()
    {
        if (!active || owner == null || !owner.ActiveAttack) return;
        Vector3 currentBase = transform.position, tip = Tip;
        Scan(currentBase,tip);
        // Sweep the complete segment through its previous-to-current displacement.
        int steps = Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(Vector3.Distance(previousBase,currentBase),Vector3.Distance(previousTip,tip)) / Mathf.Max(.01f,contactRadius)));
        for (int i = 0; i <= steps; i++)
        {
            float t = i/(float)steps;
            Scan(Vector3.Lerp(previousBase,currentBase,t),Vector3.Lerp(previousTip,tip,t));
        }
        previousBase = currentBase; previousTip = tip;
    }
    void Scan(Vector3 a, Vector3 b)
    {
        foreach (var collider in Physics.OverlapCapsule(a,b,contactRadius,targetLayers,QueryTriggerInteraction.Ignore))
        {
            var enemy = collider.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.IsDead || hit.Contains(enemy)) continue;
            Vector3 point = collider.ClosestPoint((a+b)*.5f);
            if (!CombatVisibility.Clear(owner.transform.position+Vector3.up,point,owner.transform,enemy.transform)) continue;
            if (!owner.RegisterHit(enemy)) continue;
            hit.Add(enemy);
            var result = enemy.ReceiveDamage(weaponDamage,owner.transform.position);
            if (result != HitResult.Ignored) enemy.HitVFX(point,result);
        }
    }
    void OnDrawGizmosSelected() { Gizmos.color = Color.yellow; Gizmos.DrawLine(transform.position,Tip); Gizmos.DrawWireSphere(Tip,contactRadius); }
}
