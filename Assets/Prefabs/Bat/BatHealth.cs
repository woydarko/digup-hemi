using UnityEngine;
using System.Collections;

public class BatHealth : MonoBehaviour
{
    [Header("Health & Damage")]
    public int health = 3;
    public int playerDamage = 1; // Damage yang diberikan ke Player saat sentuhan pasif
    public float deathFallSpeed = 3f; // Kekuatan gravitasi saat jatuh
    public float flashDuration = 0.1f; 
    [Header("Attack Cooldown")]
    public float hitCooldownDuration = 0.2f; // Cukup 0.2 detik (lebih cepat dari swing Player)
    private bool canBeDamaged = true;
    [Header("Death Effects")]
    public float timeToFade = 1.5f; 
    [Header("Scoring")]
    public int scoreValue = 100; // [BARU] Nilai skor yang diberikan saat musuh mati
    public GameObject floatingTextPrefab;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private BatPatrol patrolScript;
    private Transform spriteVisual; // Target flip/scale visual
    private Animator anim; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        patrolScript = GetComponent<BatPatrol>();
        anim = GetComponent<Animator>();
        spriteVisual = transform; // Asumsi SpriteRenderer ada di root object
    }

    // Fungsi dipanggil dari Player's Attack Hitbox
    public void TakeDamage(int damageAmount)
{
    // [PENTING] 1. Cek Cooldown
    if (!canBeDamaged) return; 
    if (health <= 0) return;

    // 2. Mulai Cooldown segera
    StartCoroutine(HitCooldownRoutine());

    // 3. Lanjutkan Logika Damage
    health -= damageAmount;
    
    StartCoroutine(FlashEffect()); // Tampilkan efek hit

    if (health <= 0)
    {
        Die();
    }
}

    // Fungsi dipanggil saat Bat menyentuh Player (Damage Pasif ke Player)
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Cek apakah objek yang ditabrak adalah Player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Dapatkan script PlayerHealth dari objek Player
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

            // Abaikan damage jika Player sedang menyerang (konsisten dengan Laba-laba)
            PlayerAction playerAction = collision.gameObject.GetComponent<PlayerAction>();
            if (playerAction != null && playerAction.attacking) return;

            // Jika script ditemukan, berikan damage
            if (playerHealth != null)
            {
                // Damage Pasif: Kelelawar memberikan 1 hit ke Player
                playerHealth.TakeDamage(); 
            }
        }
    }
    IEnumerator HitCooldownRoutine()
{
    canBeDamaged = false;
    yield return new WaitForSeconds(hitCooldownDuration);
    canBeDamaged = true;
}
    IEnumerator FlashEffect()
    {
        Color originalColor = sr.color;
        sr.color = Color.white; 
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor;
    }

    IEnumerator FadeOut()
    {
        float startAlpha = sr.color.a; 
        float timer = 0f;

        while (timer < timeToFade)
        {
            timer += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, 0f, timer / timeToFade);
            
            Color newColor = sr.color;
            newColor.a = newAlpha;
            sr.color = newColor;

            yield return null; 
        }
    }

    void Die()
    {
        if (floatingTextPrefab != null)
    {
        // Instansiasi (spawn) text di posisi musuh yang mati
        GameObject textInstance = Instantiate(
            floatingTextPrefab, 
            transform.position, 
            Quaternion.identity);

        // Ambil scriptnya dan set nilai teks
        FloatingScoreText fs = textInstance.GetComponent<FloatingScoreText>();
        if (fs != null)
        {
            fs.SetText(scoreValue);
        }
    }
        PlayerHealth playerHealth = Object.FindFirstObjectByType<PlayerHealth>();

    if (playerHealth != null)
    {
        playerHealth.PlayEnemyDeathSFX();
        playerHealth.AddScore(scoreValue);
    }
        // 1. Bekukan Animasi dan Kontrol
        if (anim != null) anim.enabled = false;
        if (patrolScript != null) patrolScript.enabled = false;
        
        // 2. Flip Vertikal (wajib)
        Vector3 scale = spriteVisual.localScale;
        scale.y *= -1f;
        spriteVisual.localScale = scale; // Terapkan flip ke objek visual

        // 3. Atur Fisika untuk Jatuh
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.None; // Lepaskan kunci
            rb.gravityScale = deathFallSpeed; // Aktifkan gravitasi
            rb.linearDamping = 0f;
            rb.angularDamping = 0f; 
        }
        
        // 4. Matikan Collision
        if (col != null)
        {
            col.enabled = false; 
        }

        // 5. Mulai Efek Menghilang (Fade Out)
        StartCoroutine(FadeOut()); 
        Destroy(gameObject, 2f + timeToFade);
    }
}