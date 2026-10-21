using UnityEngine;

public static class CombatFeedback
{
    public static void Show(GameObject prefab,Vector3 point,HitResult result)
    {
        if(prefab==null || result==HitResult.Ignored) return;
        var effect=Object.Instantiate(prefab,point,Quaternion.identity);
        if(result==HitResult.Blocked || result==HitResult.GuardBroken)
            foreach(var particles in effect.GetComponentsInChildren<ParticleSystem>())
            {
                var main=particles.main;
                main.startColor=result==HitResult.GuardBroken ? new Color(1,.65f,.1f) : new Color(.2f,.65f,1);
            }
        Object.Destroy(effect,3);
    }
}
