using System;
using UnityEngine;

public class PointManager : MonoBehaviour
{
    public static PointManager Instance { get; private set; }

    public int CurrentPoint;

    public event Action<int> OnPointChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddPoint(int amount)
    {
        if (amount <= 0)
            return;

        CurrentPoint += amount;
        OnPointChanged?.Invoke(CurrentPoint);

        Debug.Log($"[PointManager] +{amount} P / 현재 P: {CurrentPoint}");
    }

    public bool TrySpendPoint(int amount)
    {
        if (amount <= 0 || CurrentPoint < amount)
            return false;

        CurrentPoint -= amount;
        OnPointChanged?.Invoke(CurrentPoint);

        Debug.Log($"[PointManager] -{amount} P / 현재 P: {CurrentPoint}");
        return true;
    }
}