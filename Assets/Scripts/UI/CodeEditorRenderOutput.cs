using UnityEngine;
using UnityEngine.UI;

public sealed class CodeEditorRenderOutput : MonoBehaviour
{
    [SerializeField] private Camera editorCamera;
    [SerializeField] private RawImage output;

    private RenderTexture target;
    private bool visible;

    public void SetVisible(bool show)
    {
        visible = show && editorCamera != null && output != null;
        if (visible)
            UpdateTarget();

        bool canRender = visible && target != null;
        if (editorCamera != null)
            editorCamera.enabled = canRender;
        if (output != null)
            output.enabled = canRender;
    }

    private void LateUpdate()
    {
        if (visible)
            UpdateTarget();
    }

    private void UpdateTarget()
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (target != null && target.width == width && target.height == height && target.IsCreated())
            return;

        ReleaseTarget();
        var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = "Code Editor Camera Output",
            useMipMap = false,
            filterMode = FilterMode.Bilinear
        };

        if (!texture.Create())
        {
            Destroy(texture);
            editorCamera.enabled = false;
            output.enabled = false;
            return;
        }

        target = texture;
        editorCamera.targetTexture = target;
        output.texture = target;
        editorCamera.enabled = true;
        output.enabled = true;
    }

    private void OnDisable()
    {
        SetVisible(false);
        ReleaseTarget();
    }

    private void ReleaseTarget()
    {
        if (target == null)
            return;

        if (editorCamera != null)
            editorCamera.targetTexture = null;
        if (output != null)
            output.texture = null;
        target.Release();
        Destroy(target);
        target = null;
    }
}
