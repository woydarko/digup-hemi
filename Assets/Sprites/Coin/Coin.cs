using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Settings")]
    public int scoreValue = 10; 
    public string playerTag = "Player"; 
    // [BARU] Hubungkan Prefab Floating Text di Inspector
    [Header("Visual Feedback")]
    public GameObject floatingTextPrefab;

    private Vector3 spawnPosition;

    void Awake()
    {
        spawnPosition = transform.position;
        // Koin yang sudah diambil di run ini tidak muncul lagi setelah respawn
        if (GameData.Instance != null && GameData.Instance.collectedPickups.Contains(spawnPosition)) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                // 1. Tambahkan skor
                playerHealth.AddScore(scoreValue);
                
                // 2. [BARU] Tampilkan Floating Score Text
                SpawnFloatingText(); 
                playerHealth.PlayCoinCollectSFX();
                if (GameData.Instance != null) GameData.Instance.collectedPickups.Add(spawnPosition);
                // 3. Hancurkan koin
                Destroy(gameObject);
            }
        }
    }

    // Fungsi untuk memunculkan dan mengatur teks
    void SpawnFloatingText()
    {
        if (floatingTextPrefab != null)
        {
            // Instansiasi teks di posisi koin
            GameObject textInstance = Instantiate(
                floatingTextPrefab, 
                transform.position, 
                Quaternion.identity);

            // Set nilai teks (+10, +50, dll.)
            FloatingScoreText fs = textInstance.GetComponent<FloatingScoreText>();
            if (fs != null)
            {
                fs.SetText(scoreValue);
            }
        }
    }

}