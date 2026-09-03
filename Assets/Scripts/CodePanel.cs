using UnityEngine;
using DG.Tweening;

public class CodePanel : MonoBehaviour
{
    public float OpenSpeed = 0.5f;
    public float CloseSpeed = 0.5f;
    public bool isOpenClose = false;

    [SerializeField] private Vector2 openPosition;
    [SerializeField] private Vector2 closePosition;

    private RectTransform codePanelTransform;

    void Start()
    {
        codePanelTransform = GetComponent<RectTransform>();
        OpenClose(isOpenClose);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OpenClose(isOpenClose);
        }
    }

    public void OpenClose(bool isOpen)
    {
        isOpenClose = !isOpen;
        codePanelTransform.DOKill();

        Vector2 targetPosition = isOpen ? openPosition : closePosition;
        float duration = isOpen ? OpenSpeed : CloseSpeed;

        codePanelTransform.DOAnchorPos(targetPosition, duration).SetEase(Ease.OutCubic);
    }
}