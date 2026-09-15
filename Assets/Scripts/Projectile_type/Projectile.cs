using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float maxDistance = 10f;

    [Header("Hit")]
    [SerializeField] private Vector2 hitboxSize = Vector2.one;


    [Header("Tracking")]
    [SerializeField] private bool trackPlayer = false;
    [SerializeField] private float trackingLerp = 2f;
    [SerializeField] private float trackingSpeed = 2f;
    [Header("Tracking Slow")]
    [SerializeField] private float normalSpeed = 8f;
    [SerializeField] private float speedChangeRate = 5f;

    [Header("Explosion")]
    [SerializeField] private bool explodeOnDestroy = false;
    private Vector2 direction;
    private Transform playerTransform;

    private float damage;
    private float currentSpeed;
    private float traveledDistance;
    private bool initialized;

    public void Initialize(
        Vector2 initialDirection,
        float damage,
        Transform player
    )
    {
        direction = initialDirection.normalized;

        this.damage = damage;
        playerTransform = player;

        traveledDistance = 0f;
        currentSpeed = normalSpeed;

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        UpdateDirection();
        Move();

        if (traveledDistance >= maxDistance)
        {
            DestroyProjectile();
        }
    }

    private void UpdateDirection()
    {
        if (!trackPlayer || playerTransform == null)
            return;

        Vector2 targetDirection =
            (playerTransform.position - transform.position).normalized;

        float angle =
            Vector2.Angle(direction, targetDirection);

        direction = Vector2.Lerp(
            direction,
            targetDirection,
            trackingLerp * Time.deltaTime
        ).normalized;

        float slowAmount = Mathf.InverseLerp(
            0f,
            90f,
            angle
        );

        float targetSpeed = Mathf.Lerp(
            normalSpeed,
            trackingSpeed,
            slowAmount
        );

        currentSpeed = Mathf.Lerp(
            currentSpeed,
            targetSpeed,
            speedChangeRate * Time.deltaTime
        );
    }

    private void Move()
    {
        float moveDistance = currentSpeed * Time.deltaTime;

        transform.position +=
            (Vector3)(direction * moveDistance);

        traveledDistance += moveDistance;

        if (traveledDistance >= maxDistance)
        {
            DestroyProjectile();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable == null)
            return;

        // 일단 테스트용
        damageable.TakeDamage(1f);

        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        if (explodeOnDestroy)
        {
            Explode();
        }

        Destroy(gameObject);
    }

    private void Explode()
    {
        Debug.Log("[Projectile] 폭발!");
        // 나중에 폭발 이펙트 / 범위 데미지 구현
    }
}