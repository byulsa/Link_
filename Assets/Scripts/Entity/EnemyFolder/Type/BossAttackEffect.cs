using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class BossAttackEffect : MonoBehaviour
{
    public Color attackColor = new Color(1f, 0.17f, 0.11f);
    public Color warningColor = new Color(1f, 0.2f, 0.12f, 0.55f);
    [Min(0.01f)] public float widthMultiplier = 1f;
    [Min(0.01f)] public float fadeDuration = 0.22f;
    [Min(0.01f)] public float warningWidth = 0.09f;
    public AnimationCurve shrinkCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
}
