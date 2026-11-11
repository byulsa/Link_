using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WindowTutorial : MonoBehaviour
{
    [SerializeField] private TMP_Text body;
    [SerializeField] private TMP_Text pageNumber;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextLabel;
    [SerializeField] private Font tutorialFontSource;
    [SerializeField, TextArea(2, 5)] private string[] pages;

    private TMP_FontAsset tutorialFont;
    private int currentPage = -1;
    private int lastAdvanceFrame = -1;
    private bool completed;

    public bool IsShowing => isActiveAndEnabled && currentPage >= 0 && !completed;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);
    }

    public void ShowOnce()
    {
        if (completed || body == null || nextButton == null || pages == null || pages.Length == 0)
            return;

        if (tutorialFont == null && tutorialFontSource != null)
        {
            tutorialFont = TMP_FontAsset.CreateFontAsset(tutorialFontSource);
            if (tutorialFont != null)
            {
                body.font = tutorialFont;
                if (nextLabel != null)
                    nextLabel.font = tutorialFont;
            }
        }

        currentPage = 0;
        lastAdvanceFrame = Time.frameCount;
        RenderPage();
        gameObject.SetActive(true);
        nextButton.Select();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            NextPage();
        }
    }

    private void NextPage()
    {
        if (!IsShowing || lastAdvanceFrame == Time.frameCount)
            return;

        lastAdvanceFrame = Time.frameCount;
        if (++currentPage >= pages.Length)
        {
            completed = true;
            gameObject.SetActive(false);
            return;
        }

        RenderPage();
    }

    private void RenderPage()
    {
        body.text = pages[currentPage];
        if (pageNumber != null)
            pageNumber.text = $"{currentPage + 1} / {pages.Length}";
        if (nextLabel != null)
            nextLabel.text = currentPage + 1 == pages.Length ? "확인" : "다음";
    }

    private void OnDestroy()
    {
        if (tutorialFont != null)
            Destroy(tutorialFont);
    }
}
