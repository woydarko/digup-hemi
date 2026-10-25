using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    private GameObject enemyHitbox;   // Ditemukan di indeks 0
    private GameObject breakerHitbox; // Ditemukan di indeks 1 (sesuai urutan di Hierarchy)
    public bool attacking = false;
    // Hapus: private float timeToAttack = 0.22f;
    // Hapus: private float timer = 0f;
    private PlayerMovement movement;
    private Animator animator;
    private PlayerHealth playerHealth;
    public float attackWindupTime = 0.5f; // Durasi JEDA sebelum HITBOX aktif

void Start() 
{
    playerHealth = GetComponent<PlayerHealth>();
    // Cek keamanan: Jika setup belum lengkap, berikan error dan keluar.
    if (transform.childCount < 2) 
    {
        Debug.LogError("SETUP GAGAL! Player harus memiliki setidaknya DUA child objects (Hitbox_Enemy & Hitbox_Breaker)!");
        // return; // Opsional: Hapus return jika kamu ingin sisa fungsi Start tetap berjalan

        // Karena kita tidak bisa keluar, kita set null agar tidak crash
        enemyHitbox = null; 
        breakerHitbox = null;
    }
    else
    {
        // 1. Inisialisasi Hitbox
        enemyHitbox = transform.GetChild(0).gameObject; 
        breakerHitbox = transform.GetChild(1).gameObject;
    }
    
    // 2. Inisialisasi Komponen Dasar
    animator = GetComponent<Animator>();
    movement = GetComponent<PlayerMovement>();
    
    // 3. Deaktivasi Hitbox (Dilakukan di luar IF dengan pengecekan null)
    if (enemyHitbox != null) 
    {
        enemyHitbox.SetActive(false);
        breakerHitbox.SetActive(false);
    }
    
    // HAPUS ATAU KOMENTARI BARIS INI: attackArea.SetActive(false);
}

    void Update()
    {
        // timeScale 0 = WinPanel tampil, jangan attack
        if (Input.GetKeyDown(KeyCode.F) && !attacking && Time.timeScale > 0f)
        {
            Attack();
        }

        // HAPUS SEMUA LOGIKA TIMER YANG ADA DI SINI
        /* if (attacking)
        {
            timer += Time.deltaTime;
            if (timer >= timeToAttack)
            {
                timer = 0;
                attacking = false;
                attackArea.SetActive(attacking);
            }
        } */
    }

    // Di PlayerAction.cs

private void Attack()
{
    attacking = true;
    if (playerHealth != null)
    {
        playerHealth.PlayAttackSFX(); // Panggil fungsi suara dari PlayerHealth
    }
    // 1. [PERBAIKAN KRUSIAL] Reset status isJumping di Animator
    if (animator != null)
    {
        animator.SetBool("isJumping", false); // Paksa lompatan mati!

        // 2. Mulai animasi visual (windup)
        animator.SetTrigger("isAttacking");
    }

    // 3. Bekukan pergerakan player
    if (movement != null)
        movement.canMove = false;

    // 4. Mulai Coroutine
    StartCoroutine(WaitForAttackToFinish());
}

private IEnumerator WaitForAttackToFinish()
{
    // [1. DELAY/WINDUP] 
    yield return new WaitForSeconds(attackWindupTime);

    // --- AKTIFKAN HITBOX BARU ---
    // Aktifkan kedua Hitbox secara bersamaan
    if (enemyHitbox != null)
        enemyHitbox.SetActive(true);
    if (breakerHitbox != null)
        breakerHitbox.SetActive(true);

    // 2. TUNGGU DURASI HITBOX
    // Kita gunakan 2 siklus fisika untuk keandalan tinggi (sesuai solusi terakhir)
    yield return new WaitForFixedUpdate(); 
    yield return new WaitForFixedUpdate(); 

    // --- RESET ---
    
    // 3. [PERBAIKAN] DEAKTIVASI KEDUA HITBOX
    if (enemyHitbox != null)
        enemyHitbox.SetActive(false);
    if (breakerHitbox != null)
        breakerHitbox.SetActive(false);
    
    // 4. Tambahkan jeda 1 frame sebelum mengembalikan kontrol:
    yield return null; 
    
    // 5. Kembalikan kontrol
    attacking = false;
    if (movement != null)
        movement.canMove = true;
}
}