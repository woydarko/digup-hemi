using UnityEngine;

public class WormPatrol : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float patrolRange = 0.5f; 

    [Header("Patrol Points")]
    public Transform pointA; 
    public Transform pointB; 

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private bool movingRight = true; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb != null)
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f; // Penting untuk mencegah putaran
    }

        // (Opsional) Tentukan arah pandang awal
        FlipVisual(movingRight);
    }

    void FixedUpdate()
    {
        Patrol();
    }

    void Patrol()
    {
        if (pointA == null || pointB == null) return;

        Vector2 targetPosition;

        // 1. Tentukan target
        if (movingRight)
        {
            targetPosition = pointB.position;
        }
        else
        {
            targetPosition = pointA.position;
        }

        // 2. Cek jarak ke target untuk berbalik
        if (Vector2.Distance(transform.position, targetPosition) < patrolRange)
        {
            movingRight = !movingRight;
            FlipVisual(movingRight);
        }

        // 3. Terapkan gerakan horizontal
        float direction = movingRight ? 1f : -1f;
        
        // Penting: Hanya ubah velocity.x, biarkan velocity.y diatur oleh gravitasi
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        // 4. Mainkan animasi
        if (anim != null)
        {
             // Ganti dengan nama state animasi jalan/patroli cacingmu
             anim.Play("PatrolWalk"); 
        }
    }

    void FlipVisual(bool isMovingRight)
    {
        if (spriteRenderer != null)
        {
            // Jika sprite default menghadap KIRI, gunakan logika ini (konsisten dengan perbaikan sebelumnya)
            spriteRenderer.flipX = isMovingRight; 
        }
    }
}