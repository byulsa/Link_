using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [SerializeField] private Transform ownerTransform; // 플레이어
    [SerializeField] private Transform weaponParent;    // 무기들 담을 부모 (비우면 ownerTransform 사용)
    [SerializeField] private CodeEditor codeEditor;     // 씬의 단일 CodeEditor

    private readonly List<WeaponBase> activeWeapons = new();
    public IReadOnlyList<WeaponBase> ActiveWeapons => activeWeapons;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public WeaponBase AddWeapon(WeaponDefinition definition)
    {
        if (definition == null || definition.prefab == null)
        {
            Debug.LogWarning("[WeaponManager] 무기 정의 또는 프리팹이 없습니다.");
            return null;
        }

        Transform parent = weaponParent != null ? weaponParent : ownerTransform;
        GameObject instance = Instantiate(definition.prefab, parent);

        WeaponBase weaponBase = instance.GetComponent<WeaponBase>();
        CodeController codeController = instance.GetComponent<CodeController>();

        if (weaponBase == null)
        {
            Debug.LogWarning("[WeaponManager] 프리팹에 WeaponBase가 없습니다.");
            Destroy(instance);
            return null;
        }

        if (codeController != null && codeEditor != null)
            codeController.SetEditor(codeEditor);

        // 여러 무기가 한 지점에서 겹치지 않도록 시작 각도 분산
        float startAngle = activeWeapons.Count * (360f / (activeWeapons.Count + 1));
        weaponBase.Initialize(definition, ownerTransform, startAngle);

        activeWeapons.Add(weaponBase);
        return weaponBase;
    }
}