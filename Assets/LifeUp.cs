using UnityEngine;

public class LifeUp : MonoBehaviour
{
    [Header("Settings")]
    public int lifeAmount = 1; // Jumlah nyawa yang ditambahkan (default 1)
    public string playerTag = "Player"; 
    // [BARU] Pengaturan Efek Mengambang
    [Header("Floating Effect")]
    public float floatSpeed = 1f;        // Kecepatan mengambang
    public float floatHeight = 0.5f;     // Jarak total naik-turun (dari posisi awal)

    private Vector3 startPosition;       // Posisi awal item
    public AudioClip collectSound; 
    void Awake()
    {
        // Simpan posisi awal item saat game dimulai
        startPosition = transform.position;
    }
    void Update()
    {
        // [BARU] Logika Mengambang
        // Gunakan Mathf.Sin untuk gerakan naik-turun yang mulus
        // Waktu saat ini * kecepatan = fase osilasi
        // Hasil Sin dikalikan tinggi = jarak perpindahan dari tengah
        transform.position = startPosition + new Vector3(
            0, 
            Mathf.Sin(Time.time * floatSpeed) * floatHeight, 
            0
        );
    }
    void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag(playerTag))
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        
        if (playerHealth != null)
        {
            // 1. Tambah Nyawa
            playerHealth.AddLife(lifeAmount); 
            
            // 2. [FIX] Putar Suara Koin (Menggunakan Handler yang sudah ada)
            playerHealth.PlayCoinCollectSFX(); 

            // 3. Hancurkan item
            Destroy(gameObject); 
        }
    }
}
}