using UnityEngine;
using System.Collections.Generic;

public class Weapon : MonoBehaviour
{
    private CodeController codeController;
    private WeaponBase weaponBase;
    private readonly Dictionary<Enemy, float> nextCutHitTime = new();

    private void Awake()
    {
        codeController = GetComponent<CodeController>();

        weaponBase = GetComponent<WeaponBase>();

        Debug.Log($"[Weapon] " + $"CodeController = {codeController}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryBlockProjectile(other))
            return;

        Entity target = other.GetComponent<Entity>();
        if (target == null)
            target = other.GetComponentInParent<Entity>();

        if (target == null)
            return;

        if (!target.Is(EntityType.Enemy))
            return;

        TryHitEnemy(target);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (weaponBase == null || !weaponBase.IsContinuousCut)
            return;

        Entity target = other.GetComponent<Entity>();
        if (target == null)
            target = other.GetComponentInParent<Entity>();

        if (target != null && target.Is(EntityType.Enemy))
            TryHitEnemy(target);
    }

    private bool TryBlockProjectile(Collider2D other)
    {
        if (weaponBase == null)
            return false;

        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile == null)
            projectile = other.GetComponentInParent<Projectile>();

        return weaponBase.TryBlockProjectile(projectile);
    }

    private void TryHitEnemy(Entity target)
    {
        if (weaponBase == null)
            return;

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy == null)
            enemy = target.GetComponentInParent<Enemy>();

        if (enemy == null)
            return;

        if (weaponBase.IsContinuousCut)
        {
            if (nextCutHitTime.TryGetValue(enemy, out float nextHitTime) && Time.time < nextHitTime)
                return;

            nextCutHitTime[enemy] = Time.time + weaponBase.ContinuousCutInterval;
        }

        bool dealtDamage = codeController != null && codeController.ExecuteWeapon(target);
        weaponBase.Hit(target, !dealtDamage);
    }
}
