using System;
using UnityEditor;
using UnityEngine;

// Kept as a regular Editor tool so Unity's compilation cache never references a removed temporary script.
public static class EnemyWaveValidation
{
    [MenuItem("Tools/Validation/Validate Enemy Waves")]
    public static void Run()
    {
        ChapterData chapter = AssetDatabase.LoadAssetAtPath<ChapterData>("Assets/Scripts/Wave/Chapter_1.asset");
        Require(chapter != null && chapter.waves != null && chapter.waves.Count == 15,
            "Chapter 1 must reference 15 waves.");

        foreach (WaveData wave in chapter.waves)
        {
            Require(wave != null && wave.timeLines != null && wave.timeLines.Count > 0,
                "A wave or its timelines are missing.");
            float end = 0f;
            foreach (WaveTimeLineData timeline in wave.timeLines)
            {
                Require(Mathf.Approximately(timeline.startTime, end) && timeline.endTime > end,
                    wave.name + ": timelines must be contiguous.");
                Require(timeline.enemyData != null && timeline.enemyData.Count > 0,
                    wave.name + ": enemy data is missing.");
                foreach (EnemyData enemy in timeline.enemyData)
                {
                    Require(enemy != null && enemy.enemyPrefab != null &&
                        enemy.enemyPrefab.GetComponent<Enemy>() != null,
                        wave.name + ": an enemy prefab reference is invalid.");
                    string assetPath = AssetDatabase.GetAssetPath(enemy);
                    Require(!assetPath.Contains("Chapter1_15Waves/Enemies/"),
                        wave.name + ": use shared base enemy data rather than tier copies.");
                    if (enemy is RangedEnemyData ranged && ranged.attackType == RangedAttackType.Projectile)
                    {
                        Require(ranged.projectilePrefab != null &&
                            ranged.projectilePrefab.GetComponent<Projectile>() != null,
                            enemy.name + ": projectile prefab is missing.");
                    }
                }
                end = timeline.endTime;
            }
            Require(Mathf.Approximately(end, wave.maxTime), wave.name + ": timelines must cover the wave duration.");
        }
        Debug.Log("[EnemyWaveValidation] PASS: 15 waves and shared enemy/projectile references are valid.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[EnemyWaveValidation] " + message);
    }
}
