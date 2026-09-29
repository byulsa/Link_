using UnityEngine;

// Code의 구조/동작/역할(BlockCategory)에 따른 표시 색상을 한 곳에서 관리한다.
// CodeBlock(실제 배치된 코드)과 RewardSlotUI(보상 선택 화면) 양쪽에서 공용으로 사용해서
// 어디서 보든 같은 역할의 Code는 같은 색으로 보이게 한다.
public static class BlockCategoryColor
{
    // 프로토타입 기준 색상. 필요하면 여기 값만 바꾸면 전체에 반영된다.
    public static Color GetColor(BlockCategory category)
    {
        switch (category)
        {
            case BlockCategory.Trigger:
                return new Color(1f, 0.65f, 0.2f); // 주황 - 조건/트리거

            case BlockCategory.Action:
                return new Color(0.35f, 0.75f, 1f); // 하늘색 - 행동

            case BlockCategory.Target:
                return new Color(0.55f, 0.9f, 0.45f); // 초록 - 대상

            case BlockCategory.Modifier:
                return new Color(0.85f, 0.5f, 1f); // 보라 - 수치/연산 수정자

            case BlockCategory.Weapon:
                return new Color(1f, 0.4f, 0.4f); // 빨강 - 무기

            default:
                return Color.white;
        }
    }
}
