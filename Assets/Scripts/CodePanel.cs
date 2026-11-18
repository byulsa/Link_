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
    private GameObject shopPanel;

    private const KeyCode PreviewKey = KeyCode.Tab;

    private RectTransform codePanelTransform;
    private CodeGrid codeGrid;
    private bool isOpen;
    private bool isPreview;
    private bool isFolded;
    private CodeWorkspace workspace;
    private Vector3 expandedScale;

    private void Awake()
    {
        codePanelTransform = GetComponent<RectTransform>();
        codeGrid = GetComponentInChildren<CodeGrid>(true);
        workspace = GetComponent<CodeWorkspace>();
        expandedScale = codePanelTransform.localScale;
        codePanelTransform.localScale = expandedScale;
        if (workspace != null)
        {
            workspace.Initialize(CloseCodeEditor);
            workspace.SetOpen(false, false);
            workspace.SetVisible(false);
        }

        if (workspace == null)
            codePanelTransform.anchoredPosition = openPosition;

        codePanelTransform.localScale = Vector3.zero;
        isOpen = false;
        isOpenClose = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(PreviewKey) && WaveManager.Instance != null)
        {
            if (WaveManager.Instance.CanEditCode)
            {
                if (isOpen && !isPreview && (codeGrid == null || !codeGrid.TutorialPending))
                {
                    isFolded = !isFolded;
                    SetPanelExpanded(!isFolded, preserveView: true);
                }
            }
            else if (isOpen)
                CloseCodeEditor();
            else
                SetOpen(true, null, preview: true);

            return;
        }

        if (isOpen && !isPreview && !isFolded && Input.GetKeyDown(KeyCode.Delete))
            codeGrid?.SellSelectedBlock();

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (isOpen && isPreview)
        {
            CloseCodeEditor();
            return;
        }

        if (shopPanel != null && shopPanel.activeSelf)
        {
            CloseShop();
            return;
        }

        CloseCodeEditor();
    }

    public void CloseCodeEditor()
    {
        if (!isOpen)
            return;
        if (!isPreview && codeGrid != null && codeGrid.TutorialPending)
            return;

        WaveManager waveManager = WaveManager.Instance;
        if (isPreview || (waveManager != null && !waveManager.CanEditCode))
        {
            SetOpen(false);
            return;
        }

        if (waveManager == null)
        {
            SetOpen(false);
            Debug.LogError("[CodePanel] 코드 패널을 닫았지만 WaveManager 인스턴스를 찾지 못했습니다.", this);
            return;
        }

        SetOpen(false, waveManager.OnCodeEditorClosed);
    }

    public void CloseShop()
    {
        if (shopPanel == null || !shopPanel.activeSelf)
            return;

        WindowTutorial tutorial = shopPanel.GetComponentInChildren<WindowTutorial>();
        if (tutorial != null && tutorial.IsShowing)
            return;

        shopPanel.SetActive(false);
        WaveManager waveManager = WaveManager.Instance;
        if (waveManager == null || !waveManager.TryFinishShop())
            SetOpen(true);
    }

    public void OpenClose(bool isOpen)
    {
        SetOpen(isOpen);
    }

    public void SetOpen(bool isOpen)
    {
        SetOpen(isOpen, null);
    }

    private void SetOpen(bool isOpen, TweenCallback onComplete, bool preview = false)
    {
        this.isOpen = isOpen;
        isPreview = isOpen && (preview || (WaveManager.Instance != null && !WaveManager.Instance.CanEditCode));
        isFolded = false;
        SetPanelExpanded(isOpen, onComplete: onComplete);
        if (!isOpen)
            codeGrid?.SelectBlock(null);
    }

    private void SetPanelExpanded(bool expanded, bool preserveView = false, TweenCallback onComplete = null)
    {
        workspace?.SetOpen(expanded, isPreview, preserveView);
        if (workspace == null)
            codeGrid?.SetEditingEnabled(expanded && !isPreview);
        if (expanded)
            workspace?.SetVisible(true);

        isOpenClose = !expanded;
        codePanelTransform.DOKill();

        Vector3 targetScale = expanded ? expandedScale : Vector3.zero;

        void FinishTransition()
        {
            if (!expanded)
                workspace?.SetVisible(false);
            else if (!isPreview)
                workspace?.ShowTutorial();
            onComplete?.Invoke();
        }

        if (codePanelTransform.localScale == targetScale)
        {
            FinishTransition();
            return;
        }

        float duration = expanded ? OpenSpeed : CloseSpeed;
        codePanelTransform.DOScale(targetScale, duration)
            .SetEase(expanded ? Ease.OutCubic : Ease.InCubic)
            .OnComplete(FinishTransition);
    }
}
