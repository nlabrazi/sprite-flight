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
    public AsteroidSize asteroidSize;

    [Header("Asteroid Sprites")]
    public Sprite[] tinySprites;
    public Sprite[] smallSprites;
    public Sprite[] mediumSprites;
    public Sprite[] bigSprites;

    [Header("Size Scales")]
    public float tinyScale = 0.5f;
    public float smallScale = 1.0f;
    public float mediumScale = 1.5f;
    public float bigScale = 2.0f;

    [Header("Weighted Spawn Chances (sum doesn't have to be 1)")]
    [Range(0f, 1f)] public float tinyChance = 0.40f;
    [Range(0f, 1f)] public float smallChance = 0.30f;
    [Range(0f, 1f)] public float mediumChance = 0.20f;
    [Range(0f, 1f)] public float bigChance = 0.10f;

    [Header("Movement")]
    public float minSpeed = 50f;
    public float maxSpeed = 150f;

    [Header("Rotation")]
    public float maxSpinSpeed = 10f;

    [Header("Effects")]
    public GameObject bounceEffectPrefab;

    [Header("Boost (on Wall hit)")]
    public float boostMultiplier = 2f;
    public float boostDuration = 1f;

    private bool isBoosting = false;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (rb == null)
            return;

        asteroidSize = PickWeightedSize();
        ApplySizeAndSprite();

        ApplyRandomMovement();
        ApplyRandomSpin();
    }

    AsteroidSize PickWeightedSize()
    {
        float total = tinyChance + smallChance + mediumChance + bigChance;

        // Sécurité: si tout est à 0, fallback
        if (total <= 0f)
            return AsteroidSize.Small;

        float roll = Random.value * total;

        if (roll < tinyChance) return AsteroidSize.Tiny;
        roll -= tinyChance;

        if (roll < smallChance) return AsteroidSize.Small;
        roll -= smallChance;

        if (roll < mediumChance) return AsteroidSize.Medium;

        return AsteroidSize.Big;
    }

    void ApplySizeAndSprite()
    {
        if (sr == null)
            return;

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

        transform.localScale = new Vector3(scale, scale, 1f);

        if (pool != null && pool.Length > 0)
            sr.sprite = pool[Random.Range(0, pool.Length)];
    }

    void ApplyRandomMovement()
    {
        float size = transform.localScale.x;

        float speed = Random.Range(minSpeed, maxSpeed) / size;
        Vector2 direction = Random.insideUnitCircle.normalized;

        rb.AddForce(direction * speed);
    }

    void ApplyRandomSpin()
    {
        float spin = Random.Range(-maxSpinSpeed, maxSpinSpeed);
        rb.AddTorque(spin);
    }

    void OnCollisionEnter2D(Collision2D collision)
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

    IEnumerator BoostCoroutine()
    {
        if (rb == null)
            yield break;

        isBoosting = true;

        rb.linearVelocity *= boostMultiplier;

        yield return new WaitForSeconds(boostDuration);

        rb.linearVelocity /= boostMultiplier;

        isBoosting = false;
    }
}
