using System.Collections;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public enum AsteroidSize
    {
        Tiny,
        Small,
        Medium,
        Big
    }

    [Header("Asteroid Category (auto)")]
    [SerializeField] private AsteroidSize asteroidSize;

    [Header("Asteroid Sprites")]
    [SerializeField] private Sprite[] tinySprites;
    [SerializeField] private Sprite[] smallSprites;
    [SerializeField] private Sprite[] mediumSprites;
    [SerializeField] private Sprite[] bigSprites;

    [Header("Size Scales")]
    [SerializeField] private float tinyScale = 0.9f;
    [SerializeField] private float smallScale = 1.2f;
    [SerializeField] private float mediumScale = 1.5f;
    [SerializeField] private float bigScale = 2.0f;

    [Header("Weighted Spawn Chances")]
    [SerializeField, Range(0f, 1f)] private float tinyChance = 0.40f;
    [SerializeField, Range(0f, 1f)] private float smallChance = 0.30f;
    [SerializeField, Range(0f, 1f)] private float mediumChance = 0.20f;
    [SerializeField, Range(0f, 1f)] private float bigChance = 0.10f;

    [Header("Movement")]
    [SerializeField] private float minSpeed = 50f;
    [SerializeField] private float maxSpeed = 150f;

    [Header("Difficulty Progression")]
    [SerializeField, Min(0f)] private float speedRampPerMinute = 0.35f;
    [SerializeField, Min(1f)] private float maxSpeedMultiplier = 2.5f;
    [SerializeField, Min(0f)] private float speedAdjustRate = 8f;

    [Header("Rotation")]
    [SerializeField] private float maxSpinSpeed = 10f;

    [Header("Effects")]
    [SerializeField] private GameObject bounceEffectPrefab;

    [Header("Boost (on Wall hit)")]
    [SerializeField] private float boostMultiplier = 2f;
    [SerializeField] private float boostDuration = 1f;

    // Temporary boost state
    private bool isBoosting;
    private float baseCruiseSpeed;
    private Vector2 currentDirection = Vector2.right;

    // Cached components
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PolygonCollider2D poly;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        poly = GetComponent<PolygonCollider2D>();
    }

    private void Start()
    {
        if (rb == null || sr == null)
            return;

        asteroidSize = PickWeightedSize();
        ApplySizeAndSprite();
        ApplyRandomMovement();
        ApplyRandomSpin();
    }

    private void FixedUpdate()
    {
        if (rb == null || isBoosting)
            return;

        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude > 0.0001f)
            currentDirection = velocity.normalized;

        if (baseCruiseSpeed <= 0f)
            baseCruiseSpeed = Mathf.Max(0.1f, velocity.magnitude);

        float targetSpeed = baseCruiseSpeed * GetDifficultyMultiplier();
        float nextSpeed = Mathf.MoveTowards(velocity.magnitude, targetSpeed, speedAdjustRate * Time.fixedDeltaTime);
        rb.linearVelocity = currentDirection * nextSpeed;
    }

    // Pick asteroid size from weighted probabilities
    private AsteroidSize PickWeightedSize()
    {
        float total = tinyChance + smallChance + mediumChance + bigChance;
        if (total <= 0f) return AsteroidSize.Small;

        float roll = Random.value * total;

        if (roll < tinyChance) return AsteroidSize.Tiny;
        roll -= tinyChance;

        if (roll < smallChance) return AsteroidSize.Small;
        roll -= smallChance;

        if (roll < mediumChance) return AsteroidSize.Medium;

        return AsteroidSize.Big;
    }

    // Set sprite, scale, and collider shape
    private void ApplySizeAndSprite()
    {
        float scale;
        Sprite[] pool;
        switch (asteroidSize)
        {
            case AsteroidSize.Tiny:
                scale = tinyScale;
                pool = tinySprites;
                break;

            case AsteroidSize.Small:
                scale = smallScale;
                pool = smallSprites;
                break;

            case AsteroidSize.Medium:
                scale = mediumScale;
                pool = mediumSprites;
                break;

            case AsteroidSize.Big:
                scale = bigScale;
                pool = bigSprites;
                break;

            default:
                scale = smallScale;
                pool = smallSprites;
                break;
        }

        if (pool != null && pool.Length > 0)
            sr.sprite = pool[Random.Range(0, pool.Length)];

        transform.localScale = new Vector3(scale, scale, 1f);
        RefreshPolygonCollider();
    }

    // Rebuild polygon collider from sprite physics shape
    private void RefreshPolygonCollider()
    {
        if (poly == null || sr == null || sr.sprite == null)
            return;

        var sprite = sr.sprite;
        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount == 0)
            return;

        poly.pathCount = shapeCount;

        var points = new System.Collections.Generic.List<Vector2>(64);
        for (int i = 0; i < shapeCount; i++)
        {
            points.Clear();
            sprite.GetPhysicsShape(i, points);
            poly.SetPath(i, points);
        }
    }

    // Apply initial random velocity
    private void ApplyRandomMovement()
    {
        float size = transform.localScale.x;
        float speed = Random.Range(minSpeed, maxSpeed) / size;
        Vector2 direction = Random.insideUnitCircle.normalized;
        rb.AddForce(direction * speed);

        currentDirection = direction;
        float fallbackStartSpeed = speed * Time.fixedDeltaTime;
        baseCruiseSpeed = Mathf.Max(0.1f, rb.linearVelocity.magnitude, fallbackStartSpeed);
    }

    // Apply initial random angular velocity
    private void ApplyRandomSpin()
    {
        float spin = Random.Range(-maxSpinSpeed, maxSpinSpeed);
        rb.AddTorque(spin);
    }

    // Handle wall bounce feedback and temporary boost
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Wall"))
            return;

        if (bounceEffectPrefab != null && collision.contactCount > 0)
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            GameObject bounceEffect = Instantiate(bounceEffectPrefab, contactPoint, Quaternion.identity);
            Destroy(bounceEffect, 1f);
        }

        if (!isBoosting)
            StartCoroutine(BoostCoroutine());
    }

    // Apply short speed boost after wall hit
    private IEnumerator BoostCoroutine()
    {
        if (rb == null)
            yield break;

        isBoosting = true;

        rb.linearVelocity *= boostMultiplier;

        yield return new WaitForSeconds(boostDuration);

        rb.linearVelocity /= boostMultiplier;

        isBoosting = false;
    }

    private float GetDifficultyMultiplier()
    {
        float elapsedMinutes = Time.timeSinceLevelLoad / 60f;
        float ramp = 1f + elapsedMinutes * speedRampPerMinute;
        return Mathf.Clamp(ramp, 1f, maxSpeedMultiplier);
    }
}
