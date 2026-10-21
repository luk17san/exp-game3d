using System;
using UnityEngine;

public enum HitResult { Ignored, Hit, Blocked, GuardBroken, Killed }

[Serializable]
public class CombatGuard
{
    [Min(1)] public float maxStamina = 100;
    [Min(0)] public float regeneration = 20;
    [Min(0)] public float regenerationDelay = 1;
    [Range(0, 360)] public float angle = 120;
    [Range(0, 1)] public float reduction = 1;
    [Min(0)] public float guardDamageMultiplier = 1;
    [Min(0)] public float minimumToBlock = 20;
    [Min(0.01f)] public float breakDuration = 1.2f;
    public float Current { get; private set; }
    float resumeAt;
    public void Initialize() { Current = Mathf.Max(1, maxStamina); }
    public bool CanBlock => Current >= Mathf.Max(0.01f, minimumToBlock);
    public void Tick(float delta, float now, bool blocked)
    {
        if (!blocked && now >= resumeAt)
            Current = Mathf.Min(maxStamina, Current + Mathf.Max(0, regeneration) * delta);
    }
    public void Delay(float now) { resumeAt = now + Mathf.Max(0, regenerationDelay); }
    public bool Spend(float damage, float now)
    {
        Current = Mathf.Max(0, Current - damage * Mathf.Max(0, guardDamageMultiplier));
        Delay(now);
        return Current <= 0;
    }
    public bool Covers(Transform defender, Vector3 source)
    {
        Vector3 direction = Vector3.ProjectOnPlane(source - defender.position, Vector3.up);
        return direction.sqrMagnitude > 0.00001f &&
            Vector3.Angle(Vector3.ProjectOnPlane(defender.forward, Vector3.up), direction) <= angle * 0.5f;
    }
}

[Serializable]
public class CombatStrike
{
    public string animationState;
    [Min(0.05f)] public float duration = 0.3f;
    [Range(0, 1)] public float hitStart = 0.25f;
    [Range(0, 1)] public float hitEnd = 0.7f;
    [Min(0)] public float buffer = 0.2f;
    public CombatStrike(string state) { animationState = state; }
}

public static class CombatVisibility
{
    // Character colliders do not count as walls. World colliders, including triggers
    // only when explicitly solid, must stop a melee contact or perception ray.
    public static bool Clear(Vector3 from, Vector3 to, Transform source, Transform target)
    {
        Vector3 delta = to - from;
        if (delta.sqrMagnitude < 0.00001f) return true;
        foreach (var hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(source) || hit.transform.IsChildOf(target)) continue;
            if (hit.collider.GetComponentInParent<Enemy>() != null ||
                hit.collider.GetComponentInParent<HealthSystem>() != null) continue;
            return false;
        }
        return true;
    }
    public static bool Valid(float damage) => damage > 0 && !float.IsNaN(damage) && !float.IsInfinity(damage);
}
