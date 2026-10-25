using UnityEngine;

public class FinishFlag : MonoBehaviour
{
    // Pastikan Player Anda memiliki Tag "Player"
    [SerializeField] private string playerTag = "Player";
    private bool isFinished = false;

    // Pastikan Collider pada bendera diatur sebagai Trigger!
    void OnTriggerEnter2D(Collider2D other)
    {
        // Cek apakah yang menyentuh adalah Player dan belum selesai
        if (other.CompareTag(playerTag) && !isFinished)
        {
            isFinished = true; // Kunci agar tidak terpanggil berulang kali
            
            // Panggil GameManager untuk memicu Event Finish
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerFinished();
            }

            // Opsional: Matikan kontrol Player agar tidak bergerak lagi
            // other.GetComponent<PlayerMovement>().enabled = false; 
            
            // Opsional: Panggil animasi bendera finish (seperti yang Anda lakukan di Checkpoint)
            // GetComponent<Animator>().SetTrigger("Finish");
        }
    }
}