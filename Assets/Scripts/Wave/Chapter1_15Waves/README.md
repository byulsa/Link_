# Chapter 1: 15 waves

Chapter_1.asset is the chapter assigned in SampleScene. Each wave has three contiguous spawn phases. Spawning stops at maxTime; rewards open after every active enemy (including death effects) finishes. The existing rewards/shop flow starts the next wave. After wave 15, the HUD shows CLEAR.

All waves reference the six shared base enemy data assets under Entity/EnemyFolder/Enemy. There are no per-wave or tier EnemyData copies, and runtime scaling never changes the shared ScriptableObjects. Duplicate entries remain intentional spawn weights.

WaveManager calculates the spawn multiplier. Wave 1 starts at 1x; each subsequent wave multiplies by waveStatGrowth (default 1.2, Inspector range 1.1–1.3). A following chapter starts at the previous chapter's final multiplier times chapterStatGrowth (default 1.5). Configure followingChapters in WaveManager for chapter progression; its existing chapterData remains the starting chapter. For example: C1 W1 = 1x, W2 = 1.2x, W3 = 1.44x; if that chapter ends at W3, C2 W1 = 2.16x and W2 = 2.592x.

The multiplier raises health, damage, movement speed, attack animation speed, projectile speed and explosion damage; attack cooldown/recovery and received knockback are reduced. Rewards and attack/detection ranges retain their base values. The one-second self-destruct warning and death animations retain their original timing. Pooled enemies always reset from base data before applying the new multiplier.

SpiderNearby / SpiderDeath / SpiderBoth in the table describe timeline overrides of the same Spider_bot base asset (proximity / lethal damage / either). Wave 14 rotates all three conditions across its three phases. All warn for one second before exploding. Drones fire a straight projectile on their attack animation event.

| Wave | Spawn duration | Enemy composition | Initial phase interval |
| --- | --- | --- | --- |
| 01 | 24s | Melee | 2.40s |
| 02 | 26s | Melee, Heavy | 2.30s |
| 03 | 28s | Melee, Drone | 2.20s |
| 04 | 30s | Melee, Drone, SpiderNearby | 2.10s |
| 05 | 32s | Melee, Heavy, Drone, Guardian | 2.00s |
| 06 | 34s | Melee, Drone, SpiderDeath | 1.90s |
| 07 | 36s | Melee, Drone, Laser | 1.80s |
| 08 | 38s | Melee, Heavy, Drone, SpiderNearby | 1.70s |
| 09 | 40s | Melee, Drone, Laser, SpiderDeath | 1.60s |
| 10 | 42s | Heavy, Drone, SpiderBoth, Guardian | 1.50s |
| 11 | 44s | Melee, Drone, Laser, SpiderNearby | 1.40s |
| 12 | 46s | Melee, Heavy, Drone, Laser, SpiderDeath | 1.30s |
| 13 | 48s | Heavy, Drone, Laser, SpiderBoth, Guardian | 1.20s |
| 14 | 50s | Melee, Drone, Laser, SpiderNearby, SpiderDeath, SpiderBoth | 1.10s |
| 15 | 52s | Melee, Heavy, Drone, Laser, SpiderBoth, Guardian | 1.00s |
