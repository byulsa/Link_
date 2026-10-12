using DG.Tweening;
using UnityEngine;

public class CodePanel : MonoBehaviour
{
    public float OpenSpeed = 0.5f;
    public float CloseSpeed = 0.5f;
    public bool isOpenClose = false;

    [SerializeField]
    private Vector2 openPosition;

    [SerializeField]
    private Vector2 closePosition;

    [SerializeField]
    private GameObject shopPanel;

    private RectTransform codePanelTransform;
    private bool isOpen;

    private void Awake()
    {
        codePanelTransform = GetComponent<RectTransform>();
        codePanelTransform.anchoredPosition = closePosition;
        isOpen = false;
        isOpenClose = true;
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (shopPanel != null && shopPanel.activeSelf)
        {
            shopPanel.SetActive(false);
            SetOpen(true);
            return;
        }

        if (isOpen)
        {
            SetOpen(false);

            if (WaveManager.Instance == null)
            {
                Debug.LogError("[CodePanel] 코드 패널을 닫았지만 WaveManager 인스턴스를 찾지 못했습니다.", this);
                return;
            }

            WaveManager.Instance.NextWave();
        }
    }

    public void OpenClose(bool isOpen)
    {
        SetOpen(isOpen);
    }

    public void SetOpen(bool isOpen)
    {
        this.isOpen = isOpen;
        isOpenClose = !isOpen;
        codePanelTransform.DOKill();

        Vector2 targetPosition = isOpen ? openPosition : closePosition;
        float duration = isOpen ? OpenSpeed : CloseSpeed;

        codePanelTransform.DOAnchorPos(targetPosition, duration).SetEase(Ease.OutCubic);
    }
}
