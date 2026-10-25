using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    // --- PERSISTENCE & LIVES ---
    [Header("Life System")]
    public int maxLives = 3;
    private int currentLives; // Nilai disinkronkan dengan GameData
    [Header("Time Score")]
    public float scoreInterval = 0.2f; // Tambah skor setiap 1 detik
    public int scorePerInterval = 1;  // Nilai skor yang ditambahkan (misalnya 5)
    
    [Header("SFX")]
    public AudioClip coinCollectSFX; // Slot untuk file suara koin
    private AudioSource sfxSource;
    private float timeSinceLastScore = 0f; // Waktu pelacak
    public AudioClip jumpSFX;        
    public AudioClip attackSFX;    
    public AudioClip playerDeathSFX;  
    public AudioClip enemyDeathSFX;   
    
    [Header("Invincibility")]
    public float invincibilityDuration = 1.5f;
    public float flashRate = 0.1f;

    [Header("Death Effects")]
    public float deathAnimationTime = 1.5f; // Durasi jatuh
    public float deathBounceForce = 8f;
    
    // --- UI & RESPAWN ---
    [Header("UI Reference")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI scoreText; 
    
    [Header("Death Cutscene")]
    public GameObject retryPanel; // Panel cutscene (diaktifkan saat respawn/game over)
    public TextMeshProUGUI cutsceneLivesText; // Teks nyawa di cutscene
    
    
    private bool isInvincible = false;
    private SpriteRenderer sr; 
    private bool isDead = false; // Flag untuk mencegah double death
    public bool IsDead => isDead;

    void Awake()
    {
        // GameData hanya ada di scene MainMenu; buat otomatis saat Level1 di-Play langsung di Editor
        if (GameData.Instance == null)
        {
            new GameObject("GameData").AddComponent<GameData>();
        }
        if (GameData.Instance.lastCheckpointPosition != Vector3.zero)
        {
            // Jika ada data checkpoint, gunakan itu untuk posisi awal player
            transform.position = GameData.Instance.lastCheckpointPosition;
        }
        currentLives = GameData.Instance.currentLives;
        GameData.Instance.lastCheckpointPosition = transform.position;
        sr = GetComponent<SpriteRenderer>();
        
        sfxSource = GetComponent<AudioSource>();
    if (sfxSource == null)
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
    }
    sfxSource.playOnAwake = false;
        if(retryPanel != null) retryPanel.SetActive(false);
    }

    void Start()
    {
        UpdateLivesUI();
        UpdateScoreUI();
        isDead = false;
    }
    void Update()
{
    // Cek apakah Player sedang hidup dan tidak dalam proses kematian
    if (!isDead)
    {
        // 1. Tambahkan waktu yang berlalu
        timeSinceLastScore += Time.deltaTime; 

        // 2. Cek apakah interval sudah tercapai
        if (timeSinceLastScore >= scoreInterval)
        {
            // Reset pelacak waktu
            timeSinceLastScore = 0f; 

            // Tambahkan skor
            AddScore(scorePerInterval);
        }
    }
}
public void PlayPlayerDeathSFX()
{
    if (sfxSource != null && playerDeathSFX != null)
    {
        sfxSource.PlayOneShot(playerDeathSFX);
    }
}
public void PlayEnemyDeathSFX()
{
    if (sfxSource != null && enemyDeathSFX != null)
    {
        sfxSource.PlayOneShot(enemyDeathSFX);
    }
}
    public void PlayJumpSFX()
{
    if (sfxSource != null && jumpSFX != null)
    {
        sfxSource.PlayOneShot(jumpSFX);
    }
}
public void PlayAttackSFX()
{
    if (sfxSource != null && attackSFX != null)
    {
        sfxSource.PlayOneShot(attackSFX);
    }
}
    // Dipanggil musuh/lava (Semua sumber damage)
    public void TakeDamage()
    {
        if (isInvincible || isDead) return; 
        
        // 1. KURANGI NYAWA & SINKRONISASI
        GameData.Instance.currentLives--; 
        currentLives = GameData.Instance.currentLives;
        UpdateLivesUI(); 
        
        // 2. SELALU mulai sequence kematian yang akan mengarah ke reload
        StartCoroutine(DeathSequenceAndReload()); 
    }
    
    public void AddScore(int points)
    {
        GameData.Instance.currentScore += points;
        Debug.Log("Skor bertambah! Total: " + GameData.Instance.currentScore);
        UpdateScoreUI();
    }
    public void PlayCoinCollectSFX()
{
    if (sfxSource != null && coinCollectSFX != null)
    {
        // Putar suara secara instan dari AudioSource Player
        sfxSource.PlayOneShot(coinCollectSFX);
    }
}
    // ----------------------------------------------------------------------------------
    // --- DEATH SEQUENCE (Gabungan Cutscene & Reload) ---
    // ----------------------------------------------------------------------------------

    IEnumerator DeathSequenceAndReload()
{
    isDead = true; 
    PlayPlayerDeathSFX();
    
    // 1. Mulai efek BLINKING (Visual Feedback)
    StartCoroutine(InvincibilityCoroutine()); 
    
    // 2. Efek Hop & Jatuh
    DisableAllControls();
    StartCoroutine(ExecuteDeathHop());
    if (sr != null) sr.enabled = false;

    // Tunggu animasi jatuh selesai
    yield return new WaitForSeconds(deathAnimationTime);
    
    // 4. Tampilkan Panel Cutscene
    if (retryPanel != null)
    {
        if (cutsceneLivesText != null)
        {
            cutsceneLivesText.text = "x " + currentLives.ToString();
        }
        retryPanel.SetActive(true);
    }
    
    // 5. Jeda untuk cutscene (3 detik)
    yield return new WaitForSeconds(3f); 

    // 6. Lakukan Pengecekan Akhir untuk Tentukan Scene yang Akan Dimuat
    if (currentLives > 0)
    {
        // --- JALUR RESPAWN ---
        
        // Sembunyikan Panel Cutscene
        if (retryPanel != null) { retryPanel.SetActive(false); }

        // Reset Player State
        transform.position = GameData.Instance.lastCheckpointPosition;
        if (sr != null) sr.enabled = true;
        ResetControlsAndPhysics();

        Debug.Log("Retry! Lives left: " + currentLives + ". Reloading current scene...");
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
    else
    {
        // --- JALUR GAME OVER FINAL (Reload Main Menu) ---
        // Kita tidak perlu mengaktifkan kontrol karena scene akan segera reload
        
        if (GameData.Instance != null)
        {
            // Run diantrikan; panel simpan ke Hemi muncul di Main Menu
            if (HemiBridge.Instance != null) HemiBridge.Instance.QueueRun(GameData.Instance.currentScore, false);
            GameData.Instance.ResetRun();
        }
        
        SceneManager.LoadScene(0); // Load Main Menu
    }
}
    
    // ----------------------------------------------------------------------------------
    // --- UTILITIES ---
    // ----------------------------------------------------------------------------------

    IEnumerator ExecuteDeathHop()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Collider2D col = GetComponent<Collider2D>();
        
        if (rb != null)
        {
            if (col != null) col.enabled = false;
            
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.None;
            rb.AddForce(new Vector2(0, deathBounceForce), ForceMode2D.Impulse);
        }
        
        yield return null;
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        float timeElapsed = 0f;

        while (timeElapsed < invincibilityDuration)
        {
            if(sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(flashRate);
            timeElapsed += flashRate;
        }
        isInvincible = false;
        if (sr != null) sr.enabled = true;
    }

    public void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = GameData.Instance.currentScore.ToString("D6"); 
        }
    }
    public void AddLife(int amount)
{
    // Cek agar nyawa tidak melebihi batas maksimum (misalnya 3)
    if (GameData.Instance.currentLives < maxLives) 
    {
        GameData.Instance.currentLives += amount;
    }
    // Jika game Anda memperbolehkan nyawa lebih dari max, hapus if (currentLives < maxLives)

    // Sinkronkan nilai lokal untuk UI & logika
    currentLives = GameData.Instance.currentLives;
    
    // Update tampilan nyawa di HUD
    UpdateLivesUI(); 
    
    // Opsional: Panggil efek visual/audio 1-Up di sini
    Debug.Log("Player mendapat 1UP! Nyawa: " + currentLives);
}
    public void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = "x " + currentLives.ToString();
        }
    }
    
    private void DisableAllControls()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        
        PlayerAction action = GetComponent<PlayerAction>();

        if (movement != null) movement.enabled = false;
        if (action != null) action.enabled = false; // Cegah attack + SFX saat mati
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // Fungsi ini tidak lagi dipanggil di jalur kematian baru, tetapi tetap ada untuk keperluan lain
    private void ResetControlsAndPhysics()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Collider2D col = GetComponent<Collider2D>();

        PlayerAction action = GetComponent<PlayerAction>();

        if (movement != null) movement.enabled = true;
        if (action != null) action.enabled = true;
        if (col != null) col.enabled = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
        }
    }

    public void SetRespawnPoint(Vector3 newPosition) 
{
    // [FIX] Tulis posisi checkpoint langsung ke GameData
    if (GameData.Instance != null)
    {
        GameData.Instance.lastCheckpointPosition = newPosition;
        Debug.Log("Checkpoint Tersimpan di: " + newPosition);
    }
}
}