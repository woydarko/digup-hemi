using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AttackArea : MonoBehaviour
{
    private int damage = 3;
    

private void OnTriggerEnter2D(Collider2D collider)

{
    if (collider.CompareTag("Enemy"))
    { 
        BatHealth bat = collider.GetComponent<BatHealth>();
        SpiderHealth spider = collider.GetComponent<SpiderHealth>();
        WormHealth worm = collider.GetComponent<WormHealth>();

        // PENTING: Hapus 'return;' agar script tetap berjalan ke pengecekan Tilemap
        if (bat != null)
        {
            bat.TakeDamage(damage);
        }
        if (spider != null)
        {
            spider.TakeDamage(damage);
        }
        if (worm != null)
        {
            worm.TakeDamage(damage);
        }
    }
    // ... (Logika Cek Musuh) ...
    // Hapus logika return di sini agar Tilemap tetap dicek, kecuali ada musuh yang terkena damage 
    
    // 2. CEK TILEMAP BREAKABLE
    BreakableTilemap breakableTilemap = collider.GetComponent<BreakableTilemap>();
    
    if (breakableTilemap != null)
    {
        // Ambil referensi Tilemap untuk proses filtering
        Tilemap targetTilemap = breakableTilemap.gameObject.GetComponent<Tilemap>();
        if (targetTilemap == null) return; 

        Vector3 centerPos = transform.position;
        Vector3 correctionOffset = new Vector3(0.5f, 0.5f, 0f); 
        Vector3 correctionOffset2 = new Vector3(-0.5f, -0.5f, 0f); 

        // Definisikan 5 titik pukulan (dengan koreksi 0.5f)
        Vector3[] hitPoints = new Vector3[]
        {
            // [HANYA SATU TITIK TENGAH]
            centerPos + correctionOffset,   
            centerPos + correctionOffset2,                   
            // Titik ke Bawah (-1.0f) dan Samping tetap dipertahankan
            centerPos + correctionOffset + new Vector3(0.0f, -1.0f, 0f), // Titik 3: BAWAH (Cell di bawah kaki
            centerPos + correctionOffset2 + new Vector3(0.0f, 1.0f, 0f)
        };
        
        // Buat HashSet untuk melacak Cell mana yang sudah dipukul dalam 1 swing ini
        HashSet<Vector3Int> cellsHitThisSwing = new HashSet<Vector3Int>();

        // Kirim sinyal HitTile untuk setiap titik yang di-offset
        foreach (Vector3 hitPoint in hitPoints)
        {
            // Konversi hitPoint ke Cell Position untuk pengecekan HashSet
            Vector3Int cellPosition = targetTilemap.WorldToCell(hitPoint);
            
            // HANYA TERAPKAN DAMAGE JIKA CELL INI BELUM PERNAH DIPUKUL di SWING INI
            if (!cellsHitThisSwing.Contains(cellPosition))
            {
                breakableTilemap.HitTile(hitPoint);
                cellsHitThisSwing.Add(cellPosition); // Tandai Cell sudah dipukul
            }
        }
        
        return; // Hentikan eksekusi setelah Tilemap dipukul
    }
}
}
