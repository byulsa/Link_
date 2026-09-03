using UnityEngine;

public class Weapon : MonoBehaviour
{
    private CodeController codeController;
    private WeaponBase weaponBase;

    private void Awake()
    {
        codeController =
            GetComponent<CodeController>();

        weaponBase =
            GetComponent<WeaponBase>();

        Debug.Log(
            $"[Weapon] " +
            $"CodeController = {codeController}"
        );
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        Debug.Log(
            $"[Weapon] 충돌 발생: {other.name}"
        );

        Entity target =
            other.GetComponent<Entity>();

        if (target == null)
            return;

        Debug.Log(
            $"[Weapon] Target = {target.name}, " +
            $"Type = {target.Type}"
        );

        if (!target.Is(EntityType.Enemy))
        {
            Debug.Log(
                "[Weapon] Enemy가 아니므로 무시"
            );

            return;
        }

        // 코드가 데미지를 직접 처리했는지 확인
        bool dealtDamage = false;

        if (codeController != null)
        {
            dealtDamage =
                codeController.ExecuteWeapon(target);
        }

        // DMG 코드가 없다면 기본 데미지
        if (!dealtDamage)
        {
            if (weaponBase != null)
            {
                weaponBase.Hit(target);
            }
        }
    }
}