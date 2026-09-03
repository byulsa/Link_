using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI pointText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        PointManager.Instance.OnPointChanged += UpdateUI;
        UpdateUI(PointManager.Instance.CurrentPoint);
    }

    private void UpdateUI(int point)
    {
        pointText.text = $"{point} P";
    }
}
