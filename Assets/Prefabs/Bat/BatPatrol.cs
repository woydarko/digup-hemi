using UnityEngine;

public class BatPatrol : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    [Tooltip("Jarak toleransi musuh untuk berbalik")]
    public float patrolRange = 0.5f; 

    [Header("Patrol Points")]
    public Transform pointA; // Titik batas kiri
    public Transform pointB; // Titik batas kanan

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private bool movingRight = true; // Status arah gerak saat ini

    void Awake()
    {
        // Ambil komponen yang diperlukan
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Atur posisi awal di salah satu titik dan arah pandang awal
        if (pointA != null)
        {
             transform.position = pointA.position;
        }
        FlipVisual(movingRight);
    }

    void FixedUpdate()
    {   
        Patrol();
    }

    void Patrol()
    {
        if (pointA == null || pointB == null) return; // Pastikan titik sudah diset

        Vector2 targetPosition;

        // 1. Tentukan target saat ini
        if (movingRight)
        {
            targetPosition = pointB.position;
        }
        else
        {
            targetPosition = pointA.position;
        }

        // 2. Cek apakah musuh sudah dekat dengan target point
        if (Vector2.Distance(transform.position, targetPosition) < patrolRange)
        {
            // Sudah sampai, balik arah gerak
            movingRight = !movingRight;
            FlipVisual(movingRight);
        }

        // 3. Terapkan gerakan fisika (velocity)
        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        // 4. Mainkan animasi (sesuai setup yang kita bicarakan)
        if (anim != null)
        {
             // Asumsi state animasi bernama "PatrolWalk"
             anim.Play("PatrolWalk"); 
        }
    }

    void FlipVisual(bool isMovingRight)
    {
        // Menggunakan SpriteRenderer.flipX (konsisten dengan Player)
        if (spriteRenderer != null)
        {
            // Jika bergerak ke kanan (isMovingRight = true), flipX harus false (tidak dibalik)
            // Jika bergerak ke kiri (isMovingRight = false), flipX harus true (dibalik)
            spriteRenderer.flipX = isMovingRight;
        }
    }
    
}