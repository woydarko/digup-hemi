using UnityEngine;
using UnityEngine.SceneManagement; // PENTING: Untuk mengelola scene

public class GameManager : MonoBehaviour
{
    // Instance statis agar bisa dipanggil dari mana saja (Singleton pattern)
    public static GameManager Instance; 
    
    [Header("UI References")]
    public GameObject winPanel; // Seret WinPanel UI ke sini di Inspector

    // Scene Name
    private string currentSceneName;
    
    void Awake()
    {
        // Tetapkan Singleton
        if (Instance == null)
        {
            Instance = this;
            // Opsi: agar GameManager tetap ada di scene lain jika diperlukan
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }

        // Simpan nama scene saat ini untuk fungsi Retry
        currentSceneName = SceneManager.GetActiveScene().name;
    }

    // Dipanggil saat Player menyentuh bendera finish
    public void PlayerFinished()
    {
        Debug.Log("Game Selesai! Menampilkan Win Panel.");
        // Hentikan gerakan Player (opsional, jika Anda punya script kontrol Player)
        Time.timeScale = 0f; // Bekukan lava, skor waktu, dan player saat WinPanel tampil
        if (GameData.Instance != null)
    {
        GameData.Instance.StopBGM();
    }
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
    }

    // Dipanggil oleh tombol Retry
    public void RetryLevel()
    {
        Time.timeScale = 1f; // Kembalikan waktu normal
        if (GameData.Instance != null) GameData.Instance.ResetRun();
        SceneManager.LoadScene(currentSceneName);
    }

    // Dipanggil oleh tombol Main Menu
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Kembalikan waktu normal
        if (GameData.Instance != null) GameData.Instance.ResetRun();
        // Ganti "MainMenu" dengan nama scene Main Menu Anda
        SceneManager.LoadScene("MainMenu"); 
    }
}