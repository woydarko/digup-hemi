using UnityEngine;

public class LavaRise : MonoBehaviour
{
    [Header("Movement")]
    public float initialRiseSpeed = 0.5f; // Kecepatan awal Lava
    [Tooltip("Kecepatan Lava yang sedang berjalan")]
    public float currentRiseSpeed; 
    
    [Header("Acceleration Settings")]
    public bool enableAcceleration = true;
    [Tooltip("Kecepatan maksimum yang bisa dicapai (mis. 3.0)")]
    public float maxRiseSpeed = 3.0f; // Batas Kecepatan Maksimum
    [Tooltip("Laju pertambahan kecepatan per detik (mis. 0.05)")]
    public float accelerationRate = 0.05f; 

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb == null)
        {
            Debug.LogError("LavaRise membutuhkan Rigidbody2D di objek Tilemap Lava!");
        }
        
        // Set kecepatan awal
        currentRiseSpeed = initialRiseSpeed; 
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

            // 2. Terapkan kecepatan naik (velocity Y positif)
            rb.linearVelocity = new Vector2(0f, currentRiseSpeed);
        }
    }
}