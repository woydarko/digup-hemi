using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BreakableTilemap : MonoBehaviour
{
    // --- Configuration ---
    [Header("Settings")]
    public int hitsToBreak = 3;   
    
    [Header("Visuals")]
    public Tile[] crackStages;    
    
    [Header("Audio Settings")]
    public AudioClip damageSound;   // Suara saat Tile Terkena Damage
    public AudioClip destroySound;  // Suara saat Tile Hancur
    public float volume = 0.5f;     // Volume untuk efek suara

    // --- Internal State ---
    private Tilemap tilemap;
    private Dictionary<Vector3Int, int> tileHealths = new Dictionary<Vector3Int, int>();

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("BREAKABLE: Tilemap component is missing on this GameObject!"); 
        }
    }

    // [PUBLIC API] Fungsi ini dipanggil dari script AttackArea Player
    public void HitTile(Vector3 hitWorldPosition)
    {
        // 1. Snapping Koordinat (Fix untuk Misalignment)
        // Dapatkan World Position yang tepat di PUSAT Cell terdekat
        Vector3Int cellPosition = SnapWorldToCenterCell(hitWorldPosition);
        
        // 2. Proses Hit (Health Tracking dan Visual Update)
        ProcessTileHit(cellPosition);
    }
    
    // Mengambil World Position dan memaksanya ke Cell Center yang benar
    private Vector3Int SnapWorldToCenterCell(Vector3 worldPos)
    {
        // Mendapatkan posisi cell mentah
        Vector3Int rawCellPosition = tilemap.WorldToCell(worldPos);

        // Mendapatkan koordinat World Space yang tepat di PUSAT Cell tersebut
        Vector3 cellCenterWorld = tilemap.GetCellCenterWorld(rawCellPosition);

        // Mengkonversi kembali ke Cell Position (dijamin akurat/stabil)
        return tilemap.WorldToCell(cellCenterWorld);
    }

    private void ProcessTileHit(Vector3Int cellPosition)
    {
        // Safety check (Cek apakah ada tile setelah koordinat diperbaiki)
        if (tilemap.GetTile(cellPosition) == null) 
        {
            return; 
        }

        // 1. Dapatkan atau Inisialisasi Health Tile
        int currentHealth;
        if (!tileHealths.TryGetValue(cellPosition, out currentHealth))
        {
            currentHealth = hitsToBreak;
        }

        // 2. Kurangi Health
        currentHealth--;
        tileHealths[cellPosition] = currentHealth;

        // 3. Update Visual atau Hancurkan
        if (currentHealth <= 0)
        {
            // Tile Hancur:
            tilemap.SetTile(cellPosition, null); 
            tileHealths.Remove(cellPosition);
            
            // [AUDIO] Putar Suara Hancur
            Vector3 soundPosition = tilemap.GetCellCenterWorld(cellPosition);
            if (destroySound != null)
            {
                AudioSource.PlayClipAtPoint(destroySound, soundPosition, volume);
            }
        }
        else
        {
            // Tile Terkena Damage (Belum Hancur)
            
            // [AUDIO] Putar Suara Damage
            Vector3 soundPosition = tilemap.GetCellCenterWorld(cellPosition);
            if (damageSound != null)
            {
                AudioSource.PlayClipAtPoint(damageSound, soundPosition, volume);
            }
            
            // Tile Retak Visual
            int crackIndex = (hitsToBreak - 1) - currentHealth;
            
            if (crackStages != null && crackStages.Length > crackIndex)
            {
                tilemap.SetTile(cellPosition, crackStages[crackIndex]);
            }
        }
    }
}