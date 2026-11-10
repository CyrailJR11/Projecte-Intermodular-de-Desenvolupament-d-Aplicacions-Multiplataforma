using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EntreDosMons.Dialogue;
using EntreDosMons.Mechanics;

namespace EntreDosMons
{
    // Movimiento de plataformas para Nil y Volt: andar, saltar (con coyote time y buffer), trepar, nadar, columpiarse,
    // invertir la gravedad (Volt) y reaparecer en el último punto de control.
    // Necesita un Rigidbody2D y un Collider2D en el mismo objeto, y el sprite con el pivot en el centro.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        // Todos los personajes de la escena (lo usan la cuerda-péndulo y el HUD).
        public static readonly List<PlayerController2D> All = new List<PlayerController2D>();

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
        [Tooltip("Alto de la caja que detecta el suelo. El ancho y la posición salen del collider (en el techo también).")]
        public Vector2 groundCheckSize = new Vector2(0.4f, 0.1f);
        [Tooltip("Solo si el personaje no tiene collider: distancia del centro a los pies.")]
        public float groundCheckOffset = 0.5f;

        [Header("Gravedad invertida (Volt)")]
        public bool canFlipGravity;
        [Tooltip("Batería que gasta cada cambio de gravedad (si el personaje tiene VoltBattery).")]
        public float flipBatteryCost = 5f;

        [Header("Trepar")]
        [Tooltip("Cuerdas verticales ('|' y 'T' en el mapa) y cuerdas-péndulo ('P'). Solo Nil.")]
        public bool canClimbRopes;
        [Tooltip("Escaleras de madera ('e' en el mapa).")]
        public bool canClimbLadders = true;
        public float climbSpeed = 3.5f;

        [Header("Agua")]
        [Tooltip("Nil nada: flota, bracea con Saltar y gasta respiración. Volt no flota: se hunde y gasta batería.")]
        public bool canSwim;
        [Range(0.1f, 1f)] public float waterSpeedMultiplier = 0.6f;
        [Tooltip("Gravedad dentro del agua para quien nada.")]
        [Range(0f, 1f)] public float waterGravityMultiplier = 0.3f;
        [Tooltip("Impulso hacia arriba de cada brazada.")]
        public float swimStroke = 6f;
        [Tooltip("Velocidad máxima de hundimiento dentro del agua.")]
        public float maxSinkSpeed = 3f;

        [Header("Caídas")]
        [Tooltip("Si el personaje baja de esta altura (o sube de la siguiente) pierde una vida y reaparece.")]
        public float fallLimitBottom = -30f;
        public float fallLimitTop = 60f;

        // Lo pone CharacterSwitcher: solo el personaje controlado lee el teclado.
        public bool IsControlled { get; set; } = true;
        public bool IsGrounded { get; private set; }
        public bool IsClimbing => climb != null;
        public bool IsSwinging => swing != null;
        public bool IsInWater { get; private set; }
        public bool IsHeadUnderwater { get; private set; }
        public bool IsGravityFlipped => gravityDir < 0;
        public float GravitySign => gravityDir;
        public float Facing { get; private set; } = 1f;
        // La batería vacía de Volt lo pone a 0.6 (modo reserva).
        public float SpeedMultiplier { get; set; } = 1f;
        public bool CanAct => IsControlled && !DialogueManager.IsCinematicPlaying && Time.timeScale > 0f;
        public Vector3 RespawnPoint => respawnPoint;
        public Rigidbody2D Body => rb;
        public Vector2 Center => col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;

        public event Action<PlayerController2D> Respawned;
        public event Action<PlayerController2D> CheckpointReached;

        Rigidbody2D rb;
        SpriteRenderer sr;
        Collider2D col;
        BoxCollider2D box;
        Vector2 boxOffset;
        VoltBattery battery;
        PlayerHealth health;
        float baseGravity = 3f;
        int gravityDir = 1;
        float moveInput;
        float climbInput;
        float coyoteTimer;
        float bufferTimer;
        Vector3 respawnPoint;
        Climbable climb;
        SwingRope swing;
        Collider2D groundUnder;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponentInChildren<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            box = col as BoxCollider2D;
            if (box != null) boxOffset = box.offset;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (Mathf.Abs(rb.gravityScale) > 0.01f) baseGravity = Mathf.Abs(rb.gravityScale);
            gravityDir = 1;
            rb.gravityScale = baseGravity;
            respawnPoint = transform.position;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // Las añade el LevelBuilder al empezar, así que se buscan cuando hacen falta.
        VoltBattery Battery => battery != null ? battery : (battery = GetComponent<VoltBattery>());
        PlayerHealth Health => health != null ? health : (health = GetComponent<PlayerHealth>());

        void Update()
        {
            CheckGround();
            coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - Time.deltaTime;

            Vector2 center = Center;
            float halfHeight = col != null ? col.bounds.extents.y : 0.5f;
            bool wasInWater = IsInWater;
            IsInWater = WaterZone.Contains(center);
            if (IsInWater && !wasInWater) GameAudio.Play("chapuzon");
            IsHeadUnderwater = WaterZone.Contains(center + Vector2.up * (halfHeight * 0.8f * gravityDir));

            // En las cinemáticas de diálogo el personaje se queda quieto.
            bool canAct = CanAct;
            moveInput = canAct ? GameInput.Horizontal() : 0f;
            float vertical = canAct ? GameInput.Vertical() : 0f;
            bool jumpPressed = canAct && GameInput.JumpPressed();
            if (moveInput != 0f) Facing = Mathf.Sign(moveInput);

            if (IsSwinging)
            {
                // La cuerda-péndulo se encarga del salto para soltarse.
                bufferTimer = 0f;
            }
            else if (IsClimbing)
            {
                UpdateClimb(canAct, vertical);
                bufferTimer = 0f;
            }
            else
            {
                // Empezar a trepar: arriba delante de una cuerda/escalera, o abajo si estamos en el aire.
                if (canAct && gravityDir > 0 && (vertical > 0.5f || (vertical < -0.5f && !IsGrounded)))
                {
                    Climbable found = Climbable.Find(center, canClimbRopes, canClimbLadders);
                    if (found != null)
                    {
                        StartClimb(found);
                        jumpPressed = false;
                    }
                }
                if (!IsClimbing) UpdateJump(canAct, jumpPressed, vertical);
            }

            if (canAct && canFlipGravity && IsGrounded && !IsClimbing && !IsSwinging && GameInput.FlipGravityPressed())
            {
                if (Battery == null || Battery.TryConsume(flipBatteryCost))
                {
                    FlipGravity();
                    GameAudio.Play("gravedad");
                }
            }

            ApplyGravityScale();

            if (sr != null && moveInput != 0f)
                sr.flipX = moveInput < 0f;

            if (transform.position.y < fallLimitBottom || transform.position.y > fallLimitTop)
            {
                if (Health != null) Health.Damage(DamageType.Caida);
                else Respawn();
            }
        }

        void UpdateJump(bool canAct, bool jumpPressed, float vertical)
        {
            if (jumpPressed) bufferTimer = jumpBuffer;
            else bufferTimer -= Time.deltaTime;

            Vector2 v = rb.linearVelocity;

            // Bajar de una plataforma de madera: abajo + saltar.
            if (bufferTimer > 0f && vertical < -0.5f && IsGrounded && groundUnder != null && groundUnder.GetComponent<PlatformEffector2D>() != null)
            {
                StartCoroutine(DropThrough(groundUnder));
                bufferTimer = 0f;
                return;
            }

            if (IsInWater && canSwim && gravityDir > 0)
            {
                // Nadar: cada pulsación es una brazada. Con la cabeza fuera, salto normal para salir del agua.
                if (bufferTimer > 0f)
                {
                    v.y = IsHeadUnderwater && !IsGrounded ? swimStroke : jumpSpeed;
                    bufferTimer = 0f;
                    GameAudio.Play("salto", 0.5f);
                }
            }
            else if (bufferTimer > 0f && coyoteTimer > 0f)
            {
                // Volt dentro del agua pesa y salta menos.
                float speed = IsInWater ? jumpSpeed * 0.8f : jumpSpeed;
                v.y = speed * gravityDir;
                bufferTimer = 0f;
                coyoteTimer = 0f;
                GameAudio.Play("salto");
            }
            if (canAct && GameInput.JumpReleased() && v.y * gravityDir > 0f && !IsInWater)
                v.y *= jumpCutMultiplier;
            rb.linearVelocity = v;
        }

        void FixedUpdate()
        {
            Vector2 v = rb.linearVelocity;
            if (IsClimbing)
            {
                // Se queda centrado en la cuerda o la escalera.
                v.x = (climb.CenterX - rb.position.x) * 15f;
                v.y = climbInput * climbSpeed;
                rb.linearVelocity = v;
                return;
            }
            if (IsSwinging)
            {
                rb.AddForce(new Vector2(moveInput * swing.pushForce, 0f));
                return;
            }

            float speed = moveSpeed * SpeedMultiplier * (IsInWater ? waterSpeedMultiplier : 1f);
            v.x = Mathf.MoveTowards(v.x, moveInput * speed, acceleration * Time.fixedDeltaTime);
            if (IsInWater && v.y * gravityDir < -maxSinkSpeed) v.y = -maxSinkSpeed * gravityDir;
            rb.linearVelocity = v;
        }

        // La caja de los pies sale del collider: abajo con gravedad normal y arriba cuando Volt va por el techo.
        void CheckGround()
        {
            Vector2 point;
            Vector2 size;
            if (col != null)
            {
                Bounds b = col.bounds;
                point = new Vector2(b.center.x, gravityDir > 0 ? b.min.y : b.max.y);
                size = new Vector2(Mathf.Max(0.05f, b.size.x * 0.9f), groundCheckSize.y);
            }
            else
            {
                point = (Vector2)transform.position - new Vector2(0f, groundCheckOffset * gravityDir);
                size = groundCheckSize;
            }
            groundUnder = Physics2D.OverlapBox(point, size, 0f, groundLayers);
            // Subiendo a través de una plataforma de madera no se cuenta como suelo.
            IsGrounded = groundUnder != null && rb.linearVelocity.y * gravityDir <= 0.5f;
        }

        void ApplyGravityScale()
        {
            if (IsClimbing) { rb.gravityScale = 0f; return; }
            float multiplier = IsInWater && canSwim && gravityDir > 0 ? waterGravityMultiplier : 1f;
            rb.gravityScale = baseGravity * gravityDir * multiplier;
        }

        // ---------- Trepar ----------

        void StartClimb(Climbable target)
        {
            climb = target;
            rb.linearVelocity = Vector2.zero;
        }

        void StopClimb()
        {
            climb = null;
            climbInput = 0f;
        }

        void UpdateClimb(bool canAct, float vertical)
        {
            climbInput = vertical;
            Vector2 center = Center;

            if (canAct && GameInput.JumpButtonPressed())
            {
                StopClimb();
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, jumpSpeed * 0.8f);
                return;
            }
            if (!climb.Contains(center))
            {
                // Al llegar arriba, un empujón para subir a la plataforma.
                bool wentUp = center.y > climb.area.yMax - 0.01f;
                StopClimb();
                if (wentUp) rb.linearVelocity = new Vector2(Facing * 1.5f, 7f);
                return;
            }
            if (IsGrounded && vertical < -0.1f) StopClimb();
        }

        // ---------- Cuerda-péndulo (SwingRope la llama) ----------

        public void AttachSwing(SwingRope rope)
        {
            StopClimb();
            swing = rope;
        }

        public void DetachSwing() => swing = null;

        IEnumerator DropThrough(Collider2D platform)
        {
            if (col == null) yield break;
            Physics2D.IgnoreCollision(col, platform, true);
            yield return new WaitForSeconds(0.35f);
            if (col != null && platform != null) Physics2D.IgnoreCollision(col, platform, false);
        }

        // ---------- Gravedad ----------

        public void FlipGravity()
        {
            gravityDir = -gravityDir;
            // El collider está un poco desplazado hacia los pies: se refleja para que los pies sigan tocando el techo.
            if (box != null) box.offset = new Vector2(boxOffset.x, boxOffset.y * gravityDir);
            if (sr != null) sr.flipY = gravityDir < 0;
            ApplyGravityScale();
            IsGrounded = false;
            coyoteTimer = 0f;
        }

        public void ForceNormalGravity()
        {
            if (gravityDir < 0) FlipGravity();
        }

        // ---------- Puntos de control ----------

        public void SetCheckpoint(Vector3 position)
        {
            respawnPoint = position;
            if (DialogueManager.Instance != null) DialogueManager.Instance.ResetFalls(characterName);
            CheckpointReached?.Invoke(this);
        }

        public void Respawn()
        {
            StopClimb();
            if (swing != null) swing.Release(false);
            ForceNormalGravity();
            transform.position = respawnPoint;
            rb.position = respawnPoint;
            rb.linearVelocity = Vector2.zero;
            if (DialogueManager.Instance != null) DialogueManager.Instance.NotifyFall(characterName);
            Respawned?.Invoke(this);
        }

        // Lo usa el LevelBuilder al colocar al personaje en un nivel nuevo.
        public void ResetMotion()
        {
            StopClimb();
            if (swing != null) swing.Release(false);
            ForceNormalGravity();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            SpeedMultiplier = 1f;
        }

        // Dibuja la caja que detecta el suelo (en verde si toca suelo) en la vista Scene.
        void OnDrawGizmosSelected()
        {
            var c = GetComponent<Collider2D>();
            if (c == null) return;
            Bounds b = c.bounds;
            float g = Application.isPlaying ? gravityDir : 1f;
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(new Vector3(b.center.x, g > 0 ? b.min.y : b.max.y, 0f), new Vector3(b.size.x * 0.9f, groundCheckSize.y, 0f));
        }
    }
}
