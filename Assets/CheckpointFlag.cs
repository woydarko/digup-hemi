using UnityEngine;

public class CheckpointFlag : MonoBehaviour
{
    private Animator anim;
    private bool isActivated = false;
    [Header("Checkpoint Settings")]
public string playerTag = "Player"; // [FIX] Reintroduksi variabel ini
public SpriteRenderer visualRenderer; // [FIX] Reintroduksi variabel ini
public Color activeColor = Color.yellow;
private Color defaultColor;
    
    // Nama trigger animasi untuk menaikkan/mengubah bendera (misalnya: 'Activate')
    [SerializeField] private string activationTriggerName = "Activate"; 

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        // Setelah respawn (scene reload), tampilkan lagi bendera yang jadi checkpoint aktif
        if (GameData.Instance != null && GameData.Instance.lastCheckpointPosition == transform.position)
        {
            isActivated = true;
            ActivateVisuals();
            if (anim != null) anim.SetTrigger(activationTriggerName);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag(playerTag) && !isActivated)
    {
        if (GameData.Instance != null)
        {
            // [FIX] Tulis posisi checkpoint langsung ke data persisten
            GameData.Instance.lastCheckpointPosition = transform.position;
            
            isActivated = true;
            ActivateVisuals();
        }
        if (anim != null)
        {
            anim.SetTrigger(activationTriggerName); // <--- Baris ini menggunakan variabel tersebut
        }
    }
}
void ActivateVisuals()
{
    if (visualRenderer != null)
    {
        // Ubah warna menjadi aktif
        defaultColor = visualRenderer.color; // Simpan warna default
        visualRenderer.color = activeColor;
    }
}
}