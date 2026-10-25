using UnityEngine;

public class RandomizeAnimStart : MonoBehaviour
{
    [Range(0f, 0.5f)]
    public float speedVariation = 0.1f; // Variasi kecepatan (misal: 0.1f = speed 0.9x hingga 1.1x)

    void Start()
    {
        Animator animator = GetComponent<Animator>();

        if (animator != null)
        {
            // 1. [Acak Waktu Mulai]
            // normalizedTime adalah posisi di dalam klip (0.0 = awal, 1.0 = akhir).
            // Kita membuatnya mulai dari frame acak.
            float randomStartTime = Random.Range(0f, 1f);

            // Perintah ini membuat animasi melompat ke waktu yang acak di Layer 0.
            animator.Play(0, 0, randomStartTime);

            // 2. [Acak Kecepatan (Opsional)]
            // Memberi sedikit variasi kecepatan agar tidak semua gelembung bergerak persis sama
            animator.speed = 1f + Random.Range(-speedVariation, speedVariation);
        }
    }
}