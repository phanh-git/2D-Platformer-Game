using UnityEngine;

public class Goblin : MonoBehaviour 
{
    // Cấu hình di chuyển và giới hạn tuần tra
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float patrolDistance = 5f; // Khoảng cách tuần tra từ điểm xuất phát
    [SerializeField] private float detectionRange = 5f; // Tầm nhìn phát hiện người chơi

    // Cấu hình Layer Masks để phát hiện môi trường và người chơi
    [SerializeField] private LayerMask whatIsGround; // Layer của mặt đất
    [SerializeField] private LayerMask whatIsWall;   // Layer của tường
    [SerializeField] private LayerMask whatIsPlayer; // Layer của người chơi

    // Điểm tham chiếu và trạng thái nội bộ
    private Vector2 startPoint; // Điểm xuất phát của kẻ thù
    private Transform player;   // Tham chiếu đến người chơi
    private bool movingRight = true; // Hướng di chuyển hiện tại
    private bool playerDetected = false; // Trạng thái phát hiện người chơi

    // Tham chiếu Component
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb == null)
        {
            Debug.LogError("Goblin requires a Rigidbody2D component!", this); // Cập nhật tên script
            enabled = false; // Tắt script nếu không có Rigidbody2D
            return;
        }
        if (spriteRenderer == null)
        {
            Debug.LogError("Goblin requires a SpriteRenderer component!", this); // Cập nhật tên script
            enabled = false; // Tắt script nếu không có SpriteRenderer
            return;
        }

        startPoint = transform.position; // Lưu lại vị trí xuất phát cho tuần tra

        // Tìm người chơi bằng tag "Player"
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("Player GameObject with 'Player' tag not found! Goblin AI will not chase.", this); // Cập nhật tên script
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Luôn kiểm tra xem có thấy người chơi không
        DetectPlayer();
    }

    // FixedUpdate is used for physics calculations
    void FixedUpdate()
    {
        if (playerDetected && player != null)
        {
            ChasePlayer(); // Đuổi theo người chơi nếu đã phát hiện
        }
        else
        {
            Patrol(); // Tuần tra nếu không thấy người chơi
        }

        // Đảm bảo hướng sprite của kẻ thù khớp với hướng di chuyển
        FlipSprite();
    }

    // --- AI Logic Methods ---

    void DetectPlayer()
    {
        // Xác định hướng Raycast dựa trên hướng kẻ thù đang nhìn
        // Nếu spriteRenderer.flipX là true, kẻ thù đang nhìn sang trái
        Vector2 raycastDirection = (spriteRenderer.flipX) ? Vector2.left : Vector2.right;
        
        // Vẽ Raycast trong Scene view để dễ debug
        Debug.DrawRay(transform.position, raycastDirection * detectionRange, Color.yellow);

        // Bắn Raycast để tìm người chơi
        RaycastHit2D hit = Physics2D.Raycast(transform.position, raycastDirection, detectionRange, whatIsPlayer);

        if (hit.collider != null)
        {
            // Nếu phát hiện người chơi, kiểm tra xem có vật cản nào không (ví dụ: tường)
            RaycastHit2D obstructionCheck = Physics2D.Raycast(transform.position, raycastDirection, hit.distance, whatIsWall);
            
            if (obstructionCheck.collider == null) // Nếu không có vật cản
            {
                playerDetected = true;
                player = hit.transform; // Cập nhật tham chiếu người chơi
            }
            else // Có vật cản giữa kẻ thù và người chơi
            {
                playerDetected = false;
                player = null;
            }
        }
        else
        {
            playerDetected = false;
            player = null; // Reset player reference khi không phát hiện
        }
    }

    void Patrol()
    {
        // Kiểm tra giới hạn tuần tra
        float leftBound = startPoint.x - patrolDistance;
        float rightBound = startPoint.x + patrolDistance;

        // Bắn Raycast để kiểm tra mặt đất phía trước và tường
        // Điểm xuất phát của Raycast mặt đất được dịch sang phía trước một chút
        // để tránh Raycast bị chặn bởi chính collider của kẻ thù
        Vector2 groundCheckOrigin = transform.position + new Vector3(movingRight ? 0.4f : -0.4f, 0, 0); 
        bool groundAhead = Physics2D.Raycast(groundCheckOrigin, Vector2.down, 0.6f, whatIsGround); // 0.6f là độ dài Raycast xuống
        bool wallAhead = Physics2D.Raycast(transform.position, new Vector2(movingRight ? 1 : -1, 0), 0.5f, whatIsWall); // 0.5f là độ dài Raycast ngang

        // Vẽ các Raycast kiểm tra tuần tra
        Debug.DrawRay(groundCheckOrigin, Vector2.down * 0.6f, Color.blue); // Raycast kiểm tra mặt đất
        Debug.DrawRay(transform.position, new Vector2(movingRight ? 1 : -1, 0) * 0.5f, Color.red); // Raycast kiểm tra tường

        // Logic đổi hướng
        if (!groundAhead || wallAhead || 
            (movingRight && transform.position.x >= rightBound) || 
            (!movingRight && transform.position.x <= leftBound))
        {
            ChangeDirection(); // Đổi hướng nếu hết đất, gặp tường hoặc đạt giới hạn tuần tra
        }

        // Di chuyển kẻ thù
        float currentMoveDirectionX = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(currentMoveDirectionX * moveSpeed, rb.linearVelocity.y); // Đã sửa từ linearVelocity thành velocity
    }

    void ChasePlayer()
    {
        if (player == null)
        {
            playerDetected = false; // Không còn người chơi để đuổi theo
            return;
        }

        // Tính toán hướng tới người chơi (chỉ theo chiều ngang)
        Vector2 directionToPlayer = (player.position - transform.position);
        float currentMoveDirectionX = (directionToPlayer.x > 0) ? 1f : -1f;

        // Di chuyển kẻ thù về phía người chơi
        rb.linearVelocity = new Vector2(currentMoveDirectionX * moveSpeed, rb.linearVelocity.y); // Đã sửa từ linearVelocity thành velocity

        // Cập nhật hướng di chuyển để lật sprite đúng
        movingRight = (directionToPlayer.x > 0);
    }

    void ChangeDirection()
    {
        movingRight = !movingRight; // Đảo ngược hướng
    }

    // Phương thức lật sprite
    void FlipSprite()
    {
        if (movingRight)
        {
            spriteRenderer.flipX = false; // Không lật nếu đi sang phải
        }
        else
        {
            spriteRenderer.flipX = true; // Lật nếu đi sang trái
        }
    }

    // Phương thức vẽ Gizmos để dễ hình dung trong Editor (khi chọn Enemy GameObject)
    void OnDrawGizmos()
    {
        // Điều kiện này giúp Gizmos không bị lỗi khi script chưa được khởi tạo hoàn toàn
        // và spriteRenderer có thể là null.
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (!Application.isPlaying && spriteRenderer != null) // Chỉ chạy trong editor mode khi không chơi game
        {
            // Nếu startPoint chưa được khởi tạo, dùng vị trí hiện tại của kẻ thù
            startPoint = transform.position; 
        }

        // Vẽ giới hạn tuần tra
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector2(startPoint.x - patrolDistance, transform.position.y - 0.2f), new Vector2(startPoint.x - patrolDistance, transform.position.y + 0.2f));
        Gizmos.DrawLine(new Vector2(startPoint.x + patrolDistance, transform.position.y - 0.2f), new Vector2(startPoint.x + patrolDistance, transform.position.y + 0.2f));

        // Vẽ tầm nhìn phát hiện người chơi (Raycast màu vàng/xanh lá)
        Gizmos.color = playerDetected ? Color.green : Color.yellow;
        // Đảm bảo spriteRenderer không null trước khi truy cập flipX
        Vector2 drawRayDirection = (spriteRenderer != null && spriteRenderer.flipX) ? Vector2.left : Vector2.right;
        Gizmos.DrawRay(transform.position, drawRayDirection * detectionRange);
    }
}