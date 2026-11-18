using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerDeathTransition : MonoBehaviour
{
    [Header("Timing (seconds)")]
    [SerializeField, Min(0f)] private float glitchDuration = 0.34f;
    [SerializeField, Min(0.01f)] private float powerOffDuration = 0.12f;
    [SerializeField, Min(0f)] private float noSignalDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float powerOnDuration = 0.38f;

    [Header("Glitch")]
    [SerializeField, Range(0f, 2f)] private float colorSeparation = 1f;
    [SerializeField, Range(0f, 2f)] private float shakeStrength = 1f;
    [SerializeField, Range(0f, 1f)] private float shadowDarkening = 0.9f;

    [Header("No Signal")]
    [SerializeField] private string noSignalText = "NO SIGNAL";
    [SerializeField, Min(1f)] private float noSignalFontSize = 32f;
    [SerializeField] private Color noSignalColor = new Color(0.75f, 0.8f, 0.85f);

    private static readonly int Progress = Shader.PropertyToID("_Progress");
    private static readonly int PowerOn = Shader.PropertyToID("_PowerOn");
    private static readonly int ShutdownStart = Shader.PropertyToID("_ShutdownStart");
    private static readonly int ColorSeparation = Shader.PropertyToID("_ColorSeparation");
    private static readonly int ShakeStrength = Shader.PropertyToID("_ShakeStrength");
    private static readonly int ShadowDarkening = Shader.PropertyToID("_ShadowDarkening");
    private static float nextPowerOnDuration;
    private Texture2D snapshot;
    private Material glitchMaterial;
    private GameObject overlay;
    private float previousTimeScale;
    private bool playing;
    private PlayerHitFeedback hitFeedback;

    public void Play()
    {
        if (playing)
            return;

        playing = true;
        if (TryGetComponent(out hitFeedback))
            hitFeedback.PrepareDeathFrame();
        FindAnyObjectByType<BossEnemy>()?.ReleaseDeathSlowMotion();
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        StartCoroutine(PlayAndRestart(gameObject.scene));
    }

    private IEnumerator PlayAndRestart(Scene scene)
    {
        // Capture the rendered frame before covering it with the transition.
        yield return new WaitForEndOfFrame();
        try
        {
            snapshot = ScreenCapture.CaptureScreenshotAsTexture();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Could not capture the death frame: {exception.Message}", this);
        }

        RawImage image = ShowOverlay();
        float elapsed = 0f;
        float glitchTime = Mathf.Max(0f, glitchDuration);
        float transitionDuration = glitchTime + Mathf.Max(0.01f, powerOffDuration);
        if (glitchMaterial != null)
            glitchMaterial.SetFloat(ShutdownStart, glitchTime / transitionDuration);
        while (elapsed < transitionDuration)
        {
            SetProgress(image, elapsed / transitionDuration);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        SetProgress(image, 1f);
        if (noSignalDuration > 0f)
        {
            ShowNoSignal();
            yield return new WaitForSecondsRealtime(noSignalDuration);
        }

        nextPowerOnDuration = Mathf.Max(0.01f, powerOnDuration);
        SceneManager.sceneLoaded += OnRestartedSceneLoaded;
        if (!TryReload(scene))
        {
            SceneManager.sceneLoaded -= OnRestartedSceneLoaded;
            ClearTransition();
        }
    }

    private static void OnRestartedSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;

        SceneManager.sceneLoaded -= OnRestartedSceneLoaded;
        var effect = new GameObject("Screen Power On");
        SceneManager.MoveGameObjectToScene(effect, scene);
        var transition = effect.AddComponent<PlayerDeathTransition>();
        transition.StartCoroutine(transition.PlayPowerOn(nextPowerOnDuration));
    }

    private IEnumerator PlayPowerOn(float revealDuration)
    {
        RawImage image = ShowOverlay(powerOn: true);
        for (float elapsed = 0f; elapsed < revealDuration; elapsed += Time.unscaledDeltaTime)
        {
            SetProgress(image, elapsed / revealDuration, powerOn: true);
            yield return null;
        }
        SetProgress(image, 1f, powerOn: true);
        Destroy(gameObject);
    }

    private void ShowNoSignal()
    {
        if (string.IsNullOrWhiteSpace(noSignalText))
            return;

        var labelObject = new GameObject("No Signal", typeof(RectTransform), typeof(TextMeshProUGUI));
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        RectTransform rect = label.rectTransform;
        rect.SetParent(overlay.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        label.text = noSignalText;
        label.color = noSignalColor;
        label.fontSize = noSignalFontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private bool TryReload(Scene scene)
    {
        try
        {
            if (scene.buildIndex >= 0)
                return SceneManager.LoadSceneAsync(scene.buildIndex) != null;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(scene.path))
                return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    scene.path, new LoadSceneParameters(LoadSceneMode.Single)) != null;
#endif
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }

        Debug.LogError($"Cannot restart scene '{scene.name}' because it has no build index.", this);
        return false;
    }

    private RawImage ShowOverlay(bool powerOn = false)
    {
        overlay = new GameObject("Death Glitch", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(overlay, gameObject.scene);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var imageObject = new GameObject("Death Frame", typeof(RectTransform), typeof(RawImage));
        RawImage image = imageObject.GetComponent<RawImage>();
        RectTransform rect = image.rectTransform;
        rect.SetParent(overlay.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        image.texture = snapshot;

        Shader shader = Resources.Load<Shader>("PlayerDeathGlitch");
        if ((powerOn || snapshot != null) && shader != null && shader.isSupported)
        {
            glitchMaterial = new Material(shader);
            glitchMaterial.SetFloat(PowerOn, powerOn ? 1f : 0f);
            glitchMaterial.SetFloat(ColorSeparation, colorSeparation);
            glitchMaterial.SetFloat(ShakeStrength, shakeStrength);
            glitchMaterial.SetFloat(ShadowDarkening, shadowDarkening);
            image.material = glitchMaterial;
        }
        else if (snapshot == null)
        {
            image.color = Color.black;
        }

        return image;
    }

    private void SetProgress(RawImage image, float progress, bool powerOn = false)
    {
        if (glitchMaterial != null)
            glitchMaterial.SetFloat(Progress, progress);
        else if (powerOn)
            image.color = new Color(0f, 0f, 0f, 1f - progress);
        else if (snapshot != null)
            image.color = progress >= 1f ? Color.black : Color.white;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ClearTransition();
    }

    private void ClearTransition()
    {
        if (playing)
        {
            Time.timeScale = previousTimeScale;
            playing = false;
        }
        if (hitFeedback != null)
        {
            hitFeedback.Restore();
            hitFeedback = null;
        }
        if (overlay != null)
            Destroy(overlay);
        if (glitchMaterial != null)
            Destroy(glitchMaterial);
        if (snapshot != null)
            Destroy(snapshot);
    }
}
