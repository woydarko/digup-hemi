// FloatingScoreText.cs

using UnityEngine;
using TMPro; // Wajib untuk TextMeshPro

public class FloatingScoreText : MonoBehaviour
{
    // Nilai-nilai ini bisa diubah di Inspector Prefab
    public float moveSpeed = 1f;       // Kecepatan teks bergerak ke atas
    public float duration = 1.0f;      // Berapa lama teks akan tampil
    
    private float timeElapsed = 0f;
    private TextMeshPro tmPro;

    void Awake()
    {
        // Dapatkan referensi TextMeshPro
        tmPro = GetComponent<TextMeshPro>();
    }

    void Start()
    {
        // 1. Ini adalah Kunci FIX: Panggil Destroy di awal dengan penundaan.
        // Ini menjamin objek akan hancur setelah 'duration' detik,
        // meskipun logic di Update() bermasalah.
        Destroy(gameObject, duration); 
    }

    void Update()
    {
        // 1. Gerakkan teks ke atas
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        // 2. Hitung waktu
        timeElapsed += Time.deltaTime;

        // 3. Fade out (memudar)
        if (tmPro != null)
        {
            // Hitung faktor alfa (transparansi) berdasarkan proporsi waktu yang tersisa
            float alpha = 1f - (timeElapsed / duration);
            
            Color c = tmPro.color;
            c.a = alpha;
            tmPro.color = c;
        }
    }

    // Dipanggil oleh script musuh untuk mengatur teks dan nilai
    public void SetText(int scoreValue)
    {
        if (tmPro != null)
        {
            tmPro.text = "+" + scoreValue.ToString();
        }
    }
}