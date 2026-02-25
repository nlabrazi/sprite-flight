using UnityEngine;

public class StarfieldSpawner : MonoBehaviour
{
    [Header("Counts")]
    [SerializeField] private int smallStarCount = 450;
    [SerializeField] private int bigStarCount = 140;

    [Header("Speed")]
    [SerializeField] private float smallSpeed = 1.2f;
    [SerializeField] private float bigSpeed = 2.2f;

    [Header("Area (world units)")]
    [SerializeField] private float width = 28f;
    [SerializeField] private float height = 16f;

    [Header("Depth / Sorting")]
    [SerializeField] private float z = 0f;
    [SerializeField] private int sortingOrder = -500;

    [Header("Retro Colors")]
    [SerializeField] private Color colorA = new Color(0.25f, 1f, 0.95f, 1f);
    [SerializeField] private Color colorB = new Color(1f, 0.35f, 0.85f, 1f);
    [SerializeField] private Color colorC = new Color(1f, 0.95f, 0.35f, 1f);
    [SerializeField] private Color colorD = new Color(0.55f, 0.75f, 1f, 1f);

    // Build and configure star layers
    private void Awake()
    {
        transform.position = new Vector3(0f, 0f, z);

        var small = CreateOrGetSystem("Stars_Small");
        var big = CreateOrGetSystem("Stars_Big");

        ConfigureStars(small, smallStarCount, smallSpeed, 0.010f, 0.030f, stretch: 0.45f);
        ConfigureStars(big, bigStarCount, bigSpeed, 0.016f, 0.055f, stretch: 0.85f);
    }

    // Create child particle system or reuse existing one
    private ParticleSystem CreateOrGetSystem(string childName)
    {
        var child = transform.Find(childName);
        if (child == null)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        var ps = child.GetComponent<ParticleSystem>();
        if (ps == null) ps = child.gameObject.AddComponent<ParticleSystem>();

        var renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.sortingOrder = sortingOrder;
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        return ps;
    }

    // Configure one star particle layer
    private void ConfigureStars(
        ParticleSystem ps,
        int count,
        float speed,
        float minSize,
        float maxSize,
        float stretch
    )
    {
        float safeSpeed = Mathf.Max(0.01f, speed);
        float travelDistance = height + 2f;
        float lifetime = travelDistance / safeSpeed;
        float emissionRate = count / Mathf.Max(0.1f, lifetime);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new Color(1f, 1f, 1f, 0.9f);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width, height, 0.1f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;

        vel.x = new ParticleSystem.MinMaxCurve(0f);
        vel.y = new ParticleSystem.MinMaxCurve(-speed);
        vel.z = new ParticleSystem.MinMaxCurve(0f);

        // Add slight movement variation
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.15f;

        // Add retro arcade color mix + twinkle
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(colorA, 0f),
                new GradientColorKey(colorB, 0.28f),
                new GradientColorKey(colorC, 0.62f),
                new GradientColorKey(colorD, 1f),
            },
            new[] {
                new GradientAlphaKey(0.10f, 0f),
                new GradientAlphaKey(0.95f, 0.5f),
                new GradientAlphaKey(0.10f, 1f),
            }
        );
        col.color = grad;

        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.4f),
            new Keyframe(0.5f, 1.0f),
            new Keyframe(1f, 0.4f)
        );
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // Stretch particles to create motion lines
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.lengthScale = stretch;
        renderer.velocityScale = 0.2f;

        ps.Clear();
        ps.Simulate(lifetime, withChildren: false, restart: true, fixedTimeStep: true);
        ps.Play();
    }
}
