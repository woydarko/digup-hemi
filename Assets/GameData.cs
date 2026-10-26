using UnityEngine;
using UnityEngine.SceneManagement;

public class GameData : MonoBehaviour
{
    [Header("Scoring")]
    public int currentScore = 0; // [BARU] Variabel untuk melacak skor
    public static GameData Instance;
    [Header("BGM")]
    public AudioClip menuBGM;    // [BARU] Musik untuk Main Menu
    public AudioClip level1BGM;  // [BARU] Musik untuk Level 1
    private AudioSource bgmSource; // [BARU] Sumber Audio
    [Header("Checkpoint Data")]
public Vector3 lastCheckpointPosition; // [BARU] Posisi checkpoint terakhir yang tersimpan
    // Variable nyawa yang akan bertahan saat scene di-reload.
    public int currentLives = 3;
    public int maxLives = 3;
    // Posisi spawn koin/Cherry yang sudah diambil di run ini, supaya tidak muncul lagi setelah respawn
    public readonly System.Collections.Generic.HashSet<Vector3> collectedPickups = new System.Collections.Generic.HashSet<Vector3>();

    void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
            // BARIS KRUSIAL: Objek ini akan bertahan antar scene.
            DontDestroyOnLoad(gameObject); 
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
            bgmSource.loop = true; // Musik latar harus loop
            if (bgmSource != null && !bgmSource.isPlaying)
            lastCheckpointPosition = Vector3.zero; // Setel ke (0,0,0) atau posisi awal yang aman
    {
        // Panggil BGM untuk Scene yang sedang aktif
        PlayBGM(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
        }
        else
        {
            // Hancurkan duplikat jika objek ini sudah ada.
            Destroy(gameObject); 
        }
    }
    void OnEnable()
    {
        // Duplikat (yang akan di-Destroy) jangan ikut subscribe: bgmSource-nya null
        if (Instance == this) SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGM(scene.name);
    }
    public void PlayBGM(string sceneName)
    {
        AudioClip targetClip = null;

        // Tentukan musik berdasarkan nama scene
        if (sceneName.Contains("MainMenu")) // Asumsi Scene Menu bernama "MainMenu"
        {
            targetClip = menuBGM;
        }
        else if (sceneName.Contains("Level1")) // Asumsi Scene Level 1 bernama "Level1"
        {
            targetClip = level1BGM;
        }

        // !isPlaying: putar ulang clip yang sama setelah StopBGM (mis. Retry setelah menang)
        if (targetClip != null && (bgmSource.clip != targetClip || !bgmSource.isPlaying))
        {
            bgmSource.clip = targetClip;
            bgmSource.Play();
        }
        else if (targetClip == null)
        {
             // Opsional: Hentikan musik jika tidak ada BGM yang ditentukan untuk scene ini
             // bgmSource.Stop(); 
        }
    }
    public void ResetScore()
    {
        currentScore = 0;
    }
    // Fungsi untuk mereset nyawa saat Game Over total (nyawa dari 0 kembali ke max)
    public void ResetLives()
    {
        currentLives = maxLives;
    }
    // Reset semua progress run (skor, nyawa, checkpoint) untuk permainan baru
    public void ResetRun()
    {
        ResetScore();
        ResetLives();
        lastCheckpointPosition = Vector3.zero;
        collectedPickups.Clear();
    }
    public void StopBGM()
{
    if (bgmSource != null && bgmSource.isPlaying)
    {
        bgmSource.Stop();
    }
}
}