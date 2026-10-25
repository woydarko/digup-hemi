using UnityEngine;

// Kamera mengikuti Player secara vertikal dengan smoothing; X tetap (level vertikal)
public class VerticalCameraFollow : MonoBehaviour
{
    [Header("Follow")]
    [Tooltip("Geser target kamera ke atas dari Player, supaya area di atas lebih kelihatan")]
    public float yOffset = 1.5f;
    [Range(0.05f, 1f)]
    [Tooltip("Makin besar makin halus (tapi makin telat)")]
    public float smoothTime = 0.25f;
    [Tooltip("Kamera tidak turun di bawah posisi awalnya")]
    public bool clampToStartY = true;

    private Transform player;
    private PlayerHealth playerHealth;
    private float minY;
    private float velocityY;

    void Start()
    {
        minY = transform.position.y;

        // Snap langsung ke Player saat scene mulai/respawn, tanpa animasi dari bawah
        if (FindPlayer())
        {
            Vector3 p = transform.position;
            p.y = TargetY();
            transform.position = p;
        }
    }

    void LateUpdate()
    {
        if (player == null && !FindPlayer()) return;

        // Saat mati, Player jatuh tanpa collider; kamera diam supaya tidak ikut turun
        if (playerHealth != null && playerHealth.IsDead) return;

        Vector3 pos = transform.position;
        pos.y = Mathf.SmoothDamp(pos.y, TargetY(), ref velocityY, smoothTime);
        transform.position = pos;
    }

    float TargetY()
    {
        float y = player.position.y + yOffset;
        return clampToStartY ? Mathf.Max(y, minY) : y;
    }

    bool FindPlayer()
    {
        GameObject go = GameObject.FindWithTag("Player");
        if (go == null) return false;
        player = go.transform;
        playerHealth = go.GetComponent<PlayerHealth>();
        return true;
    }
}
