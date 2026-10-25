using UnityEngine;

public class VerticalCameraFollow : MonoBehaviour
{
    [Header("Floor Setup")]
    // Seret objek penanda lantai (Floor Markers) ke sini, dari bawah ke atas.
    public Transform[] floorMarkers;
    public float yThreshold = 1f;       // Jarak aman Player dari target sebelum memicu naik.

    [Header("Movement")]
    [Range(0.1f, 2f)] // Memudahkan penyesuaian di Inspector
    public float cameraSmoothTime = 0.5f; // <--- UBAH NILAI INI
    // Nilai 0.5f adalah default, coba naikkan ke 1.0f atau 1.5f untuk lebih halus

    private int currentFloorIndex = 0;
    private Vector3 velocity = Vector3.zero; // Digunakan untuk Vector3.SmoothDamp

    private Transform player; // Cache, supaya tidak FindWithTag tiap frame

    void LateUpdate()
    {
        if (floorMarkers == null || floorMarkers.Length == 0) return;
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null) return;
            player = playerObject.transform;
        }


        // 1. Cek apakah Player sudah melewati batas lantai saat ini
        CheckForNewFloor(player.position.y);

        // 2. Tentukan posisi target kamera saat ini
        float targetY = floorMarkers[currentFloorIndex].position.y;

        // 3. Terapkan posisi baru secara mulus
        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = new Vector3(currentPosition.x, targetY, currentPosition.z);

        // Gunakan SmoothDamp untuk gerakan yang halus
        transform.position = Vector3.SmoothDamp(
            currentPosition,
            targetPosition,
            ref velocity,
            cameraSmoothTime // <--- Menggunakan variabel smoothing
        );
    }

    void CheckForNewFloor(float playerY)
    {
        if (floorMarkers.Length == 0) return;

        // --- CHECK UPWARD (NAIK) ---
        // Cek apakah Player sudah berada di atas batas lantai berikutnya.
        if (currentFloorIndex < floorMarkers.Length - 1)
        {
            float nextFloorY = floorMarkers[currentFloorIndex + 1].position.y;

            // Pindah ke lantai atas jika Player melewati batas lantai berikutnya minus threshold
            if (playerY > nextFloorY - yThreshold)
            {
                currentFloorIndex++;
                Debug.Log("Kamera naik ke Lantai: " + currentFloorIndex);
                return;
            }
        }

        // --- CHECK DOWNWARD (TURUN) ---
        // Cek apakah Player sudah jatuh di bawah batas lantai saat ini.
        if (currentFloorIndex > 0)
        {
            float currentFloorY = floorMarkers[currentFloorIndex].position.y;

            // Pindah ke lantai bawah jika Player jatuh di bawah batas lantai saat ini minus threshold
            if (playerY < currentFloorY - yThreshold)
            {
                currentFloorIndex--;
                Debug.Log("Kamera turun ke Lantai: " + currentFloorIndex);
            }
        }
    }
}