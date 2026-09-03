using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Player Bounds")]
    [SerializeField] private Transform player;

    [SerializeField] private float playerPadding = 0.5f;

    private void Awake()
    {
        if (!targetCamera)
            targetCamera = GetComponent<Camera>();

        if (!player)
            Debug.LogWarning("[CameraController] Player reference is missing.");
    }

    public Vector2 ClampPlayerPosition(Vector2 position)
    {
        if (!targetCamera)
            return position;

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        Vector3 cameraPosition = targetCamera.transform.position;

        float minX = cameraPosition.x - halfWidth + playerPadding;
        float maxX = cameraPosition.x + halfWidth - playerPadding;

        float minY = cameraPosition.y - halfHeight + playerPadding;
        float maxY = cameraPosition.y + halfHeight - playerPadding;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}