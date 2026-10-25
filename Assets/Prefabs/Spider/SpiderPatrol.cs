using UnityEngine;

public class SpiderPatrol : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float patrolRange = 0.5f; 
    [Header("Initialization")]
public bool startMovingRight = true;
    [Header("Patrol Points")]
    public Transform pointA; 
    public Transform pointB; 

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private bool movingRight;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb != null)
        movingRight = startMovingRight;
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f; // Penting untuk mencegah putaran
    }
        // Tentukan arah pandang awal
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

        // 3. Terapkan gerakan horizontal (Y-velocity diurus gravitasi)
        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        // 4. Mainkan animasi
        if (anim != null)
        {
             // Ganti dengan nama state animasi jalan/patroli laba-laba
             anim.Play("PatrolWalk"); 
        }
    }

    void FlipVisual(bool isMovingRight)
    {
        if (spriteRenderer != null)
        {
            // Logika flip konsisten dengan Worm dan Player
            spriteRenderer.flipX = isMovingRight; 
        }
    }
}