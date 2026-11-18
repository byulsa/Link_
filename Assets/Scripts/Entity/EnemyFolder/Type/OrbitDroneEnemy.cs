using UnityEngine;

public sealed class OrbitDroneEnemy : Enemy
{
    [SerializeField] private SpriteRenderer blade;
    [SerializeField, Min(0.1f)] private float orbitRadius = 2.3f;
    [SerializeField, Min(1f)] private float orbitDegreesPerSecond = 130f;
    [SerializeField, Min(0.1f)] private float bladeHitRadius = 0.42f;
    [SerializeField, Min(0.1f)] private float hitCooldown = 0.65f;
    [SerializeField, Min(0f)] private float approachTolerance = 0.25f;

    private Collider2D playerCollider;
    private float angle;
    private float nextHitTime;

    protected override void OnEnable()
    {
        base.OnEnable();
        angle = Random.Range(0f, 360f);
        nextHitTime = 0f;
        playerCollider = PlayerTarget != null ? PlayerTarget.GetComponent<Collider2D>() : null;

        if (blade != null)
        {
            blade.enabled = true;
            PositionBlade();
        }
    }

    protected override void Update()
    {
        if (blade != null)
            blade.enabled = !IsDead;

        if (IsDead || IsKnockedBack || PlayerTransform == null)
            return;

        float distance = Vector2.Distance(transform.position, PlayerTransform.position);
        if (distance > orbitRadius + approachTolerance)
            MoveToPlayer();
        else if (distance < orbitRadius - approachTolerance)
            MoveAwayFromPlayer();
        else
            StopMovement();
    }

    private void FixedUpdate()
    {
        if (IsDead || blade == null || PlayerTransform == null)
            return;

        angle = Mathf.Repeat(angle + orbitDegreesPerSecond * Time.fixedDeltaTime, 360f);
        PositionBlade();

        if (Time.time < nextHitTime)
            return;

        Vector2 bladePosition = blade.transform.position;
        Vector2 hitPoint = playerCollider != null && playerCollider.enabled
            ? playerCollider.ClosestPoint(bladePosition)
            : (Vector2)PlayerTransform.position;

        if ((hitPoint - bladePosition).sqrMagnitude > bladeHitRadius * bladeHitRadius)
            return;

        nextHitTime = Time.time + hitCooldown;
        PlayerTarget.TakeDamage(BaseDamage);
    }

    private void PositionBlade()
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians)) * orbitRadius;
        blade.transform.SetPositionAndRotation(transform.position + offset,
            Quaternion.Euler(0f, 0f, angle));
    }
}
