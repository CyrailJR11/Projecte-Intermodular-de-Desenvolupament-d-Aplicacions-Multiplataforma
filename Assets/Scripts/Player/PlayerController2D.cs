using UnityEngine;
using EntreDosMons.Dialogue;

namespace EntreDosMons
{
    // Movimiento de plataformas para Nil y Volt: andar, saltar (con coyote time y buffer), caer y reaparecer.
    // Volt puede invertir la gravedad con F si marcas "Can Flip Gravity".
    // Necesita un Rigidbody2D y un Collider2D en el mismo objeto, y el sprite con el pivot en el centro.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Tooltip("\"Nil\" o \"Volt\": se usa para los comentarios de las caídas del sistema de diálogos.")]
        public string characterName = "Nil";

        [Header("Movimiento")]
        [Tooltip("Casillas por segundo.")]
        public float moveSpeed = 5f;
        public float acceleration = 60f;
        public float jumpSpeed = 12f;
        [Tooltip("Al soltar el salto antes de tiempo la velocidad se multiplica por esto (salto corto).")]
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
        [Tooltip("Margen para saltar justo después de salir de un borde.")]
        public float coyoteTime = 0.1f;
        [Tooltip("Margen para pulsar saltar un poco antes de tocar el suelo.")]
        public float jumpBuffer = 0.1f;

        [Header("Suelo")]
        [Tooltip("Capas que cuentan como suelo (la del Tilemap). No incluyas la capa del propio personaje.")]
        public LayerMask groundLayers;
        public Vector2 groundCheckSize = new Vector2(0.4f, 0.1f);
        [Tooltip("Distancia del centro del personaje a sus pies (0.5 = media casilla).")]
        public float groundCheckOffset = 0.5f;

        [Header("Gravedad invertida (Volt)")]
        public bool canFlipGravity;

        [Header("Caídas")]
        [Tooltip("Si el personaje baja de esta altura (o sube de la siguiente) reaparece en el último punto de control.")]
        public float fallLimitBottom = -30f;
        public float fallLimitTop = 60f;

        // Lo pone CharacterSwitcher: solo el personaje controlado lee el teclado.
        public bool IsControlled { get; set; } = true;
        public bool IsGrounded { get; private set; }
        public float GravitySign => rb.gravityScale >= 0f ? 1f : -1f;

        Rigidbody2D rb;
        SpriteRenderer sr;
        float moveInput;
        float coyoteTimer;
        float bufferTimer;
        Vector3 respawnPoint;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponentInChildren<SpriteRenderer>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            respawnPoint = transform.position;
        }

        void Update()
        {
            float g = GravitySign;
            Vector2 feet = (Vector2)transform.position - new Vector2(0f, groundCheckOffset * g);
            IsGrounded = Physics2D.OverlapBox(feet, groundCheckSize, 0f, groundLayers) != null;
            coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - Time.deltaTime;

            // En las cinemáticas de diálogo el personaje se queda quieto.
            bool canAct = IsControlled && !DialogueManager.IsCinematicPlaying;
            moveInput = canAct ? GameInput.Horizontal() : 0f;

            if (canAct && GameInput.JumpPressed()) bufferTimer = jumpBuffer;
            else bufferTimer -= Time.deltaTime;

            Vector2 v = rb.linearVelocity;
            if (bufferTimer > 0f && coyoteTimer > 0f)
            {
                v.y = jumpSpeed * g;
                bufferTimer = 0f;
                coyoteTimer = 0f;
            }
            if (canAct && GameInput.JumpReleased() && v.y * g > 0f)
                v.y *= jumpCutMultiplier;
            rb.linearVelocity = v;

            if (canAct && canFlipGravity && IsGrounded && GameInput.FlipGravityPressed())
                FlipGravity();

            if (sr != null && moveInput != 0f)
                sr.flipX = moveInput < 0f;

            if (transform.position.y < fallLimitBottom || transform.position.y > fallLimitTop)
                Respawn();
        }

        void FixedUpdate()
        {
            Vector2 v = rb.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, moveInput * moveSpeed, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = v;
        }

        public void FlipGravity()
        {
            rb.gravityScale = -rb.gravityScale;
            if (sr != null) sr.flipY = rb.gravityScale < 0f;
        }

        public void SetCheckpoint(Vector3 position)
        {
            respawnPoint = position;
            if (DialogueManager.Instance != null) DialogueManager.Instance.ResetFalls(characterName);
        }

        public void Respawn()
        {
            transform.position = respawnPoint;
            rb.linearVelocity = Vector2.zero;
            if (rb.gravityScale < 0f) FlipGravity();
            if (DialogueManager.Instance != null) DialogueManager.Instance.NotifyFall(characterName);
        }

        // Dibuja la caja que detecta el suelo (en verde si toca suelo) en la vista Scene.
        void OnDrawGizmosSelected()
        {
            float g = Application.isPlaying ? GravitySign : 1f;
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position - new Vector3(0f, groundCheckOffset * g, 0f), groundCheckSize);
        }
    }
}
