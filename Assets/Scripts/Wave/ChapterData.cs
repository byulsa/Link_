using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Chapter_", menuName = "Wave/Chapter", order = 0)]
public class ChapterData : ScriptableObject
{
    public string chapterName;
    public List<WaveData> waves;
}