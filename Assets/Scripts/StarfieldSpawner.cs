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

    // Build and configure star layers
    private void Awake()
    {
        transform.position = new Vector3(0f, 0f, z);

        var small = CreateOrGetSystem("Stars_Small");
        var big = CreateOrGetSystem("Stars_Big");

        ConfigureStars(small, smallStarCount, smallSpeed, 0.02f, 0.07f, stretch: 0.6f);
        ConfigureStars(big, bigStarCount, bigSpeed, 0.05f, 0.14f, stretch: 1.2f);
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
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        main.startLifetime = 999f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new Color(0.92f, 0.97f, 1f, 0.9f);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

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

        // Add twinkle over lifetime
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.8f,0.9f,1f), 0f),
                new GradientColorKey(new Color(1f,1f,1f), 0.5f),
                new GradientColorKey(new Color(0.8f,0.9f,1f), 1f),
            },
            new[] {
                new GradientAlphaKey(0.15f, 0f),
                new GradientAlphaKey(0.95f, 0.5f),
                new GradientAlphaKey(0.15f, 1f),
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
        ps.Emit(count);
        ps.Play();
    }
}
