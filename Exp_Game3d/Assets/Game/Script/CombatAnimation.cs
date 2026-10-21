using UnityEngine;

// An explicit full-body action layer prevents locomotion and old action layers
// from overwriting one another. Locomotion keeps its original controller.
public sealed class CombatAnimation
{
    readonly Animator animator;
    readonly int layer;
    float[] savedWeights;
    public CombatAnimation(Animator animator)
    {
        this.animator = animator;
        layer = animator != null ? animator.GetLayerIndex("CombatV2") : -1;
    }
    public void Play(string state, float duration, float clipLength = 0, float normalizedStart = 0)
    {
        if (animator == null || layer < 0 || !animator.HasState(layer, Animator.StringToHash("CombatV2." + state))) return;
        if (savedWeights == null)
        {
            savedWeights = new float[animator.layerCount];
            for (int i = 1; i < savedWeights.Length; i++)
            {
                savedWeights[i] = animator.GetLayerWeight(i);
                if (i != layer) animator.SetLayerWeight(i, 0);
            }
        }
        animator.SetLayerWeight(layer, 1);
        // State speed uses actionSpeed; never change Animator.speed globally.
        animator.SetFloat("actionSpeed", clipLength > 0 ? clipLength / Mathf.Max(.05f, duration) : 1);
        animator.Play("CombatV2." + state, layer, normalizedStart);
    }
    public void Stop()
    {
        if (animator == null || layer < 0) return;
        animator.SetLayerWeight(layer, 0);
        if (savedWeights != null)
            for (int i = 1; i < savedWeights.Length; i++)
                if (i != layer) animator.SetLayerWeight(i, savedWeights[i]);
        savedWeights = null;
    }
}
