using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float minSize = 0.5f;
    public float maxSize = 2.0f;
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    public float maxSpinSpeed = 10f;

    Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Randomize size
        float randomSize = Random.Range(minSize, maxSize);
        transform.localScale = new Vector3(randomSize, randomSize, 1);

        // Adjust speed based on size (smaller obstacles move faster)
        float randomSpeed = Random.Range(minSpeed, maxSpeed) / randomSize;
        Vector2 randomDirection = Random.insideUnitCircle;

        // Apply movement and spin
        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(randomDirection * randomSpeed);

        // Randomize spin
        float randomSpin = Random.Range(-maxSpinSpeed, maxSpinSpeed);
        rb.AddTorque(randomSpin);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
