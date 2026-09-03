using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 25f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 75f;

    [Header("Camera Bounds")]
    [SerializeField] private CameraController cameraController;

    public float Speed => speed;

    public Rigidbody2D rb;

    private Vector2 moveInput;

    private void Start()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();

        if (!cameraController)
            cameraController = FindFirstObjectByType<CameraController>();
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(x, y).normalized;
    }

    private void FixedUpdate()
    {
        Vector2 targetVelocity = moveInput * Speed;

        float rate = moveInput.sqrMagnitude > 0f
            ? acceleration
            : deceleration;

        rb.linearVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            rate * Time.fixedDeltaTime
        );

        ClampToCamera();
    }

    private void ClampToCamera()
    {
        if (!cameraController)
            return;

        Vector2 clampedPosition = cameraController.ClampPlayerPosition(rb.position);

        if (clampedPosition != rb.position)
        {
            Vector2 velocity = rb.linearVelocity;

            if (!Mathf.Approximately(clampedPosition.x, rb.position.x))
                velocity.x = 0f;

            if (!Mathf.Approximately(clampedPosition.y, rb.position.y))
                velocity.y = 0f;

            rb.linearVelocity = velocity;
            rb.position = clampedPosition;
        }
    }
}
