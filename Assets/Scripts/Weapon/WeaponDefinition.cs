using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/Weapon Definition", fileName = "NewWeaponDefinition")]
public class WeaponDefinition : ScriptableObject
{
    public string weaponName;

    [Header("Base Stats")]
    public float damage = 10f;
    public float distance = 100f;
    public float rotationSpeed = 180f;
    public float size = 1f;

    [Header("Visual")]
    public Sprite icon;
    public GameObject prefab;
}