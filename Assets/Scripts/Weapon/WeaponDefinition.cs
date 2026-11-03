using DG.Tweening;
using UnityEngine;

public enum WeaponSpecialEffect
{
    None,
    Shockwave,
    ContinuousCut,
    ChainLightning,
    ProjectileDefense
}

[CreateAssetMenu(menuName = "Weapon/Weapon Definition", fileName = "NewWeaponDefinition")]
public class WeaponDefinition : ScriptableObject
{
    public string weaponName;
    public string displayText;
    [TextArea(2, 5)]
    public string description;

    [Header("Base Stats")]
    public float damage = 10f;
    public float distance = 100f;
    public float rotationSpeed = 180f;
    public float size = 1f;
    [Header("Orbit Motion")]
    public float rotationAccelerationDuration = 0.4f;
    public Ease rotationAccelerationEase = Ease.OutCubic;
    public float rotationDecelerationDuration = 0.4f;
    public Ease rotationDecelerationEase = Ease.OutCubic;

    [Header("Special Effect")]
    public WeaponSpecialEffect specialEffect;
    [Min(0f)] public float shockwaveRadius = 2f;
    [Min(0f)] public float shockwaveDamageMultiplier = 0.5f;
    [Min(0.01f)] public float continuousCutInterval = 0.25f;
    [Min(0f)] public float chainLightningRadius = 3f;
    [Min(0)] public int chainLightningTargets = 3;
    [Range(0f, 1f)] public float chainLightningDamageMultiplier = 0.7f;

    [Header("Visual")]
    public Sprite icon;
    public GameObject prefab;
}
