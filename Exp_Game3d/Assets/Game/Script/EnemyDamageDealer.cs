using UnityEngine;
public class EnemyDamageDealer : MonoBehaviour
{
    [SerializeField, Min(0)] float weaponLength = .8f;
    [SerializeField, Min(0)] float weaponDamage = 10;
    [SerializeField, Min(.01f)] float contactRadius = .08f;
    Enemy owner;
    bool active, dealt;
    Vector3 previous, previousTip;
    void Awake() { owner = GetComponentInParent<Enemy>(); }
    public void StartDealDamage() { if (active || owner == null || !owner.IsSwinging) return; active = true; dealt = false; previous = transform.position; previousTip = previous-transform.up*weaponLength; }
    public void EndDealDamage() { active = false; }
    void OnDisable() { active = false; }
    void LateUpdate()
    {
        if (!active || dealt || owner == null || !owner.IsSwinging) return;
        Vector3 position = transform.position;
        Vector3 tip=position-transform.up*weaponLength;
        int steps = Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(Vector3.Distance(previous,position),Vector3.Distance(previousTip,tip))/Mathf.Max(.01f,contactRadius)));
        for (int i=0;i<=steps && !dealt;i++)
        {
            Vector3 a = Vector3.Lerp(previous,position,i/(float)steps);
            Vector3 b=Vector3.Lerp(previousTip,tip,i/(float)steps);
            foreach (var collider in Physics.OverlapCapsule(a,b,contactRadius,1<<8,QueryTriggerInteraction.Ignore))
            {
                var health = collider.GetComponentInParent<HealthSystem>();
                if (health == null || health.IsDead) continue;
                Vector3 point = collider.ClosestPoint(a);
                if (!CombatVisibility.Clear(owner.transform.position+Vector3.up,point,owner.transform,health.transform)) continue;
                dealt = true;
                var result = health.ReceiveDamage(weaponDamage,owner.transform.position);
                if (result != HitResult.Ignored) health.HitVFX(point,result);
                break;
            }
        }
        previous = position; previousTip=tip;
    }
}
