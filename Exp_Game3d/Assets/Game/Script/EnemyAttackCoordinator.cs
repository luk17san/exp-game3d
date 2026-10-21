using System.Collections.Generic;
using UnityEngine;

public static class EnemyAttackCoordinator
{
    sealed class Request { public Enemy enemy; public Transform player; public float since; public bool granted; }
    static readonly List<Request> requests = new List<Request>();
    public const int MaximumAttackers = 2;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { requests.Clear(); }
    public static bool RequestAttack(Enemy enemy, Transform player)
    {
        requests.RemoveAll(r => r.enemy == null || !r.enemy.isActiveAndEnabled || r.enemy.IsDead || r.player == null);
        Request own = requests.Find(r => r.enemy == enemy);
        if (own != null && own.player != player) { requests.Remove(own); own = null; }
        if (own == null) { own = new Request { enemy = enemy, player = player, since = Time.time }; requests.Add(own); }
        if (own.granted) return true;
        int occupied = requests.FindAll(r => r.player == player && r.granted).Count;
        if (occupied >= MaximumAttackers) return false;
        Request first = null;
        foreach (var r in requests)
        {
            if (r.player != player || r.granted || !r.enemy.ReadyToAttack) continue;
            if (first == null || r.since < first.since ||
                (r.since == first.since && (r.enemy.transform.position-player.position).sqrMagnitude < (first.enemy.transform.position-player.position).sqrMagnitude)) first = r;
        }
        if (first != own) return false;
        own.granted = true; return true;
    }
    public static void Release(Enemy enemy) { requests.RemoveAll(r => r.enemy == enemy); }
}
