using UnityEngine;

public class LavaRise : MonoBehaviour
{
    [Header("Movement")]
    public float initialRiseSpeed = 0.35f; // Kecepatan awal Lava (~8 detik sampai spawn Level1)
    [Tooltip("Kecepatan Lava yang sedang berjalan")]
    public float currentRiseSpeed;

    [Header("Acceleration Settings")]
    public bool enableAcceleration = true;
    [Tooltip("Kecepatan dasar maksimum (tanpa catch-up)")]
    public float maxRiseSpeed = 0.9f; // Batas Kecepatan Maksimum
    [Tooltip("Laju pertambahan kecepatan per detik")]
    public float accelerationRate = 0.003f;

    [Header("Catch-up (Rubber Band)")]
    [Tooltip("Jika Player lebih dari jarak ini di atas permukaan lava, lava ngebut mengejar")]
    public float catchUpDistance = 8f;
    [Tooltip("Tambahan kecepatan per unit jarak di atas catchUpDistance")]
    public float catchUpFactor = 0.25f;
    public float catchUpMaxSpeed = 3f;

    private Rigidbody2D rb;
    private Collider2D surface; // Collider trigger lava, untuk tinggi permukaan
    private Transform player;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb == null)
        {
            Debug.LogError("LavaRise membutuhkan Rigidbody2D di objek Tilemap Lava!");
        }
        
        // Set kecepatan awal
        currentRiseSpeed = initialRiseSpeed;

        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            if (c.isTrigger) surface = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Cek Tag Player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(); 
            }
        }
    }
    
    void FixedUpdate()
    {
        // Pastikan Rigidbody ada dan sudah disetel ke Kinematic
        if (rb != null)
        {
            // 1. Logika Akselerasi
            if (enableAcceleration)
            {
                // Tambah kecepatan naik seiring waktu (linear)
                currentRiseSpeed += accelerationRate * Time.fixedDeltaTime; 
                
                // [FIX UTAMA] Batasi kecepatan agar tidak melebihi batas yang masuk akal
                currentRiseSpeed = Mathf.Min(currentRiseSpeed, maxRiseSpeed);
            }

            // 2. Catch-up: kejar Player yang sudah jauh di atas (di luar layar)
            float speed = currentRiseSpeed;
            if (player == null)
            {
                GameObject go = GameObject.FindWithTag("Player");
                if (go != null) player = go.transform;
            }
            if (player != null && surface != null)
            {
                float gap = player.position.y - surface.bounds.max.y;
                if (gap > catchUpDistance)
                {
                    speed = Mathf.Min(speed + (gap - catchUpDistance) * catchUpFactor, catchUpMaxSpeed);
                }
            }

            // 3. Terapkan kecepatan naik (velocity Y positif)
            rb.linearVelocity = new Vector2(0f, speed);
        }
    }
}