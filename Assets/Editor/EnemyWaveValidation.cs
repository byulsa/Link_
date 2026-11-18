using System;
using System.Collections.Generic;
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

        HashSet<EnemyData> previousEnemies = new();
        foreach (WaveData wave in chapter.waves)
        {
            Require(wave != null && wave.timeLines != null && wave.timeLines.Count > 0,
                "A wave or its timelines are missing.");
            if (wave.boss != null)
                ValidateBossWave(wave, previousEnemies);

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
                    if (wave.boss == null)
                        previousEnemies.Add(enemy);
                }
                end = timeline.endTime;
            }
            Require(Mathf.Approximately(end, wave.maxTime), wave.name + ": timelines must cover the wave duration.");
        }
        Require(chapter.waves[14].boss != null, "Chapter 1 must end with a boss wave.");
        Debug.Log("[EnemyWaveValidation] PASS: Chapter 1 waves, boss, and enemy references are valid.");
    }

    private static void ValidateBossWave(WaveData wave, HashSet<EnemyData> previousEnemies)
    {
        GameObject prefab = wave.boss.enemyPrefab;
        Require(prefab != null && prefab.GetComponent<BossEnemy>() != null,
            wave.name + ": boss prefab is missing.");
        Require(Mathf.Approximately(wave.boss.maxHealth, 5000f), wave.name + ": boss health must be 5000.");
        var timeline = wave.timeLines[0];
        Require(timeline.enemyData != null && previousEnemies.SetEquals(timeline.enemyData)
            && timeline.enemyData.Count == previousEnemies.Count,
            wave.name + ": reinforcements must contain each earlier enemy exactly once.");

        Animator animator = prefab.GetComponent<Animator>();
        Require(animator != null && animator.runtimeAnimatorController != null, wave.name + ": boss animator is missing.");
        bool laserEvent = false;
        bool slashEvent = false;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            Require(AnimationUtility.GetObjectReferenceCurveBindings(clip).Length > 0,
                clip.name + ": boss sprite frames are missing.");
            foreach (AnimationEvent frameEvent in AnimationUtility.GetAnimationEvents(clip))
            {
                laserEvent |= frameEvent.functionName == nameof(BossEnemy.AnimationEvent_BossLaser);
                slashEvent |= frameEvent.functionName == nameof(BossEnemy.AnimationEvent_BossSlash);
            }
        }
        Require(laserEvent && slashEvent, wave.name + ": boss attack animation events are missing.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[EnemyWaveValidation] " + message);
    }
}
