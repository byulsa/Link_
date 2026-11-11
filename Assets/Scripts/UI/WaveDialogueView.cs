using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveDialogueView : MonoBehaviour
{
    private const float CharacterInterval = 0.025f;
    private const float FadeDuration = 0.16f;

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI speaker;
    [SerializeField] private TextMeshProUGUI body;
    [SerializeField] private Image portrait;
    [SerializeField] private Button continueButton;
    [SerializeField] private ScrollRect scroll;

    private TMP_FontAsset defaultFont;
    private float defaultFontSize;
    private TMP_FontAsset generatedFont;
    private bool viewReady;
    private WaveDialogue.Line[] lines;
    private Action completed;
    private int lineIndex;
    private int lastAdvanceFrame = -1;
    private float elapsed;
    private float revealDuration;

    public bool IsPlaying => completed != null;

    private bool HasCustomView => panelRoot != null && body != null && continueButton != null &&
        panelRoot != gameObject && !transform.IsChildOf(panelRoot.transform) &&
        body.transform.IsChildOf(panelRoot.transform) && continueButton.transform.IsChildOf(panelRoot.transform);

    public bool TryPlay(WaveDialogue dialogue, Font sourceFont, Action onCompleted)
    {
        if (!isActiveAndEnabled || !HasCustomView)
            return false;

        if (!viewReady)
            InitializeView(sourceFont);

        lines = dialogue.lines;
        lineIndex = -1;
        completed = onCompleted;
        lastAdvanceFrame = Time.frameCount;
        panelRoot.SetActive(true);
        ShowNextLine();
        if (IsPlaying)
            continueButton.Select();
        return true;
    }

    private void Update()
    {
        if (!IsPlaying)
            return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Continue();
        }

        if (!IsPlaying || elapsed >= revealDuration)
            return;

        elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, revealDuration);
        ApplyCharacterFade();
    }

    public void Continue()
    {
        if (!IsPlaying || lastAdvanceFrame == Time.frameCount)
            return;

        lastAdvanceFrame = Time.frameCount;
        if (elapsed < revealDuration)
        {
            elapsed = revealDuration;
            ApplyCharacterFade();
            return;
        }

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        while (++lineIndex < lines.Length)
        {
            WaveDialogue.Line line = lines[lineIndex];
            if (string.IsNullOrWhiteSpace(line?.text))
                continue;

            if (speaker != null)
                speaker.text = line.speaker ?? string.Empty;

            if (portrait != null)
            {
                portrait.sprite = line.portrait;
                portrait.gameObject.SetActive(line.portrait != null);
            }

            body.font = line.font != null ? line.font : defaultFont;
            body.fontSize = line.fontSize > 0f ? line.fontSize : defaultFontSize;
            body.text = line.text;
            Canvas.ForceUpdateCanvases();
            if (scroll != null)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
            }
            body.ForceMeshUpdate();

            int visibleCount = 0;
            for (int i = 0; i < body.textInfo.characterCount; i++)
            {
                if (body.textInfo.characterInfo[i].isVisible)
                    visibleCount++;
            }

            elapsed = 0f;
            revealDuration = visibleCount == 0 ? 0f : (visibleCount - 1) * CharacterInterval + FadeDuration;
            ApplyCharacterFade();
            return;
        }

        panelRoot.SetActive(false);
        lines = null;
        Action onCompleted = completed;
        completed = null;
        onCompleted?.Invoke();
    }

    private void ApplyCharacterFade()
    {
        SetCharacterAlpha(body.textInfo);
        body.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void SetCharacterAlpha(TMP_TextInfo info)
    {
        int visibleIndex = 0;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible)
                continue;

            float progress = (elapsed - visibleIndex++ * CharacterInterval) / FadeDuration;
            byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(progress) * byte.MaxValue);
            Color32[] colors = info.meshInfo[character.materialReferenceIndex].colors32;
            for (int vertex = character.vertexIndex; vertex < character.vertexIndex + 4; vertex++)
                colors[vertex].a = alpha;
        }
    }

    private void Awake()
    {
        if (HasCustomView)
            panelRoot.SetActive(false);
    }

    private void InitializeView(Font sourceFont)
    {
        defaultFontSize = body.fontSize;
        if (sourceFont != null)
            generatedFont = TMP_FontAsset.CreateFontAsset(sourceFont);
        defaultFont = generatedFont != null ? generatedFont : body.font;
        if (defaultFont == null)
            defaultFont = TMP_Settings.defaultFontAsset;

        body.OnPreRenderText += SetCharacterAlpha;
        continueButton.onClick.AddListener(Continue);
        viewReady = true;
    }

    private void OnDestroy()
    {
        if (viewReady && body != null)
            body.OnPreRenderText -= SetCharacterAlpha;
        if (viewReady && continueButton != null)
            continueButton.onClick.RemoveListener(Continue);
        if (generatedFont != null)
            Destroy(generatedFont);
    }
}
