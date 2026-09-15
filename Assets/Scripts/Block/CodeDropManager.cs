using UnityEngine;

public class CodeDropManager : MonoBehaviour
{
    public static CodeDropManager Instance { get; private set; }

    [SerializeField] private CodeGrid grid;
    [SerializeField] private CodeEditor editor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void TryDrop(CodeDropTable table)
    {
        if (table == null)
            return;

        // 전체 드랍 확률
        if (Random.Range(0f, 100f) >= table.dropChance)
            return;

        // 어떤 블록인지 선택
        CodeDropTable.DropEntry entry =
            SelectDrop(table);

        if (entry == null || entry.block == null)
            return;

        BlockDefinition definition =
            entry.block;

        // 실제 값 결정
        int value = 0;

        if (definition.hasValue)
        {
            value = Random.Range(
                definition.minValue,
                definition.maxValue + 1
            );
        }

        // Grid에 들어갈 공간 확인
        int blockWidth =
            GetBlockWidth(definition, value);

        if (!grid.TryFindEmptyPosition(
                blockWidth,
                out Vector2Int position))
        {
            Debug.Log(
                $"[CodeDrop] 공간 부족: {definition.displayText}"
            );

            return;
        }

        // 실제 블록 생성
        editor.CreateDropBlock(
            definition,
            position,
            value
        );

    }

    private CodeDropTable.DropEntry SelectDrop(
        CodeDropTable table)
    {
        float totalWeight = 0f;

        foreach (var entry in table.drops)
        {
            if (entry == null ||
                entry.block == null ||
                entry.weight <= 0f)
            {
                continue;
            }

            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float random =
            Random.Range(0f, totalWeight);

        foreach (var entry in table.drops)
        {
            if (entry == null ||
                entry.block == null ||
                entry.weight <= 0f)
            {
                continue;
            }

            random -= entry.weight;

            if (random <= 0f)
                return entry;
        }

        return null;
    }

    private int GetBlockWidth(
        BlockDefinition definition,
        int value)
    {
        if (!definition.hasValue)
            return definition.displayText.Length;

        return
            definition.displayText.Length +
            value.ToString().Length;
    }
}