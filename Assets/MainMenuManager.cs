using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections; // Untuk Coroutine
using TMPro; // Untuk TextMeshPro

public class MainMenuManager : MonoBehaviour
{

    [Header("Cursor Settings")]
    public RectTransform cursorArrow; // Seret objek CursorArrow ke sini
    public float cursorOffsetX = -20f;
    public float cursorOffsetY = 0f;
    public float cursorMoveSpeed = 0.1f;
    public float cursorFlickerSpeed = 0.2f; // <--- BARU: Kecepatan kedipan kursor
    private Image cursorImage; 
    private Coroutine currentFlickerCoroutine; // Untuk menghentikan kedipan sebelumnya
    [Header("UI Elements")]
    public Button startButton;
    public Button leaderboardButton;
    public Button exitButton;
    

    [Header("Audio")]
    public AudioClip selectSound;
    public AudioClip clickSound;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        if (cursorArrow != null)
        {
            cursorImage = cursorArrow.GetComponent<Image>();
            if (cursorImage == null)
            {
                Debug.LogWarning("CursorArrow missing Image component!");
            }
        }
    }

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

#if UNITY_WEBGL
        // Application.Quit tidak berfungsi di browser
        if (exitButton != null) exitButton.gameObject.SetActive(false);
#endif

        // Pastikan cursorArrow ada
        if (cursorArrow != null)
        {
            cursorArrow.gameObject.SetActive(true); // Aktifkan objek kursor
            // Set posisi awal kursor di samping tombol Start Game
            MoveCursorToButton(startButton); 
        }
        if (startButton != null)
        {
            startButton.Select(); // Fokuskan tombol Start Game
            lastSelected = startButton.gameObject;
        }

        // Tambahkan Listener untuk setiap tombol agar kursor bergerak saat dipilih
        //startButton?.GetComponent<Button>().onSelect.AddListener(() => OnButtonSelected(startButton));
        //optionsButton?.GetComponent<Button>().onSelect.AddListener(() => OnButtonSelected(optionsButton));
        //exitButton?.GetComponent<Button>().onSelect.AddListener(() => OnButtonSelected(exitButton));
    }

    // Dipanggil saat tombol dipilih (highlighted)
    public void OnButtonSelected(Button selectedButton)
    {
        MoveCursorToButton(selectedButton);
        PlaySelectSound(); // Putar suara select saat kursor bergerak
        if (cursorImage != null)
        {
            // Hentikan kedipan sebelumnya jika ada
            if (currentFlickerCoroutine != null)
            {
                StopCoroutine(currentFlickerCoroutine);
            }
            // Mulai kedipan baru
            currentFlickerCoroutine = StartCoroutine(FlickerCursor(cursorImage));
        }
    }

    private GameObject lastSelected;

    // Kursor ikut pindah saat pilihan menu berubah (panah keyboard atau klik)
    void Update()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return;
        GameObject current = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        if (current == null || current == lastSelected) return;
        lastSelected = current;
        Button button = current.GetComponent<Button>();
        if (button == startButton || button == leaderboardButton || button == exitButton) OnButtonSelected(button);
    }

    // Fungsi untuk memindahkan kursor ke posisi tombol yang dipilih
    void MoveCursorToButton(Button targetButton)
    {
        if (cursorArrow == null || targetButton == null) return;

        // Dapatkan RectTransform dari teks tombol, bukan dari button itu sendiri
        // Karena teks button biasanya yang menjadi referensi visual
        TextMeshProUGUI targetText = targetButton.GetComponentInChildren<TextMeshProUGUI>();
        if (targetText == null) return;
        // Taruh kursor di kiri tepi teks yang dirender (bukan tengah rect), supaya tidak menimpa teks di resolusi apa pun
        Canvas.ForceUpdateCanvases();
        targetText.ForceMeshUpdate();
        Bounds bounds = targetText.textBounds;
        Vector3 newCursorPosition = targetText.transform.TransformPoint(new Vector3(bounds.min.x, bounds.center.y, 0f));
        float scale = cursorArrow.lossyScale.x; // offset dalam unit canvas
        newCursorPosition.x += (cursorOffsetX - cursorArrow.rect.width * cursorArrow.pivot.x) * scale;
        newCursorPosition.y += cursorOffsetY * scale;

        // Mulai Coroutine untuk animasi pergerakan kursor
        if (gameObject.activeInHierarchy) // Pastikan GameObject aktif sebelum Coroutine
        {
            StartCoroutine(AnimateCursor(newCursorPosition));
        }
    }

    // Coroutine untuk menganimasikan pergerakan kursor
    IEnumerator AnimateCursor(Vector3 targetPosition)
    {
        if (cursorArrow == null) yield break;

        float elapsedTime = 0f;
        Vector3 startPosition = cursorArrow.position;

        while (elapsedTime < cursorMoveSpeed)
        {
            cursorArrow.position = Vector3.Lerp(startPosition, targetPosition, (elapsedTime / cursorMoveSpeed));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cursorArrow.position = targetPosition; // Pastikan posisi akhir tepat
    }
    IEnumerator FlickerCursor(Image imageToFlicker)
    {
        while (true)
        {
            if (imageToFlicker != null)
            {
                imageToFlicker.enabled = !imageToFlicker.enabled; // Toggle enable/disable
            }
            yield return new WaitForSeconds(cursorFlickerSpeed);
        }
    }
    public void ExitGame()
{
    Debug.Log("Exiting Game...");
    PlayClickSound();
    
    // Perintah untuk keluar dari aplikasi. Hanya berfungsi di build game (.exe),
    // tidak berfungsi saat diuji di Unity Editor.
    Application.Quit(); 
    
    // Baris ini opsional, hanya untuk testing di Editor:
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
}
    // --- Fungsi Button (onClick) ---
    public void StartGame()
    {
        Debug.Log("Starting Game...");
        PlayClickSound();
        Time.timeScale = 1f;
        if (GameData.Instance != null) GameData.Instance.ResetRun();
        StartCoroutine(LoadAfterClick("Level1"));
    }

    // Tunggu SFX klik selesai, karena AudioSource menu ikut hancur saat scene diganti
    IEnumerator LoadAfterClick(string sceneName)
    {
        if (clickSound != null) yield return new WaitForSecondsRealtime(clickSound.length);
        SceneManager.LoadScene(sceneName);
    }

    public void OpenLeaderboard()
    {
        PlayClickSound();
        if (HemiBridge.Instance != null) HemiBridge.Instance.ShowLeaderboard();
    }


    // --- Fungsi Audio ---
    private void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    public void PlaySelectSound()
    {
        if (audioSource != null && selectSound != null)
        {
            audioSource.PlayOneShot(selectSound);
        }
    }
}