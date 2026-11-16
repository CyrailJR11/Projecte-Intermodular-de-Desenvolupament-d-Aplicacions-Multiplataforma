using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EntreDosMons.Dialogue;
using EntreDosMons.Mechanics;

namespace EntreDosMons
{
    // ================================================================================================
    // [POO · Herencia + Abstracción] Clase base ABSTRACTA de los dos personajes.
    // Tiene todo lo que Nil y Volt hacen igual (andar, saltar, trepar escaleras, interactuar, reaparecer...)
    // y deja "huecos" (miembros abstract / virtual) para lo que cada uno hace a su manera:
    //   · abstract  → cada subclase TIENE que decirlo: nombre, mundo, habilidades.
    //   · virtual   → hay un comportamiento por defecto que la subclase PUEDE cambiar (override):
    //                 qué pasa al entrar en el agua, la habilidad especial, chocar con una caja...
    // No se puede poner un PlayerCharacter en un objeto: hay que usar NilCharacter o VoltCharacter.
    //
    // [POO · Encapsulamiento] Los ajustes son campos privados [SerializeField] (se ven en el Inspector pero
    // ningún otro script los puede cambiar) y el estado se expone con propiedades de solo lectura
    // (IsGrounded, IsClimbing...). Solo la propia clase decide cómo cambia su estado.
    // ================================================================================================
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CharacterHealth))]
    public abstract class PlayerCharacter : MonoBehaviour
    {
        // Todos los personajes de la escena (lo usan la cuerda-péndulo, el HUD y el cambio de personaje).
        public static readonly List<PlayerCharacter> All = new List<PlayerCharacter>();

        // ---------- Lo que define a cada personaje (abstract: obligatorio en cada subclase) ----------
        public abstract string DisplayName { get; }
        public abstract World HomeWorld { get; }
        public abstract CharacterAbility Abilities { get; }
        public bool Has(CharacterAbility ability) => (Abilities & ability) == ability;

        [Header("Movimiento")]
        [Tooltip("Casillas por segundo.")]
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float acceleration = 60f;
        [SerializeField] float jumpSpeed = 12f;
        [Tooltip("Al soltar el salto antes de tiempo la velocidad se multiplica por esto (salto corto).")]
        [Range(0f, 1f)] [SerializeField] float jumpCutMultiplier = 0.5f;
        [SerializeField] float coyoteTime = 0.1f;
        [SerializeField] float jumpBuffer = 0.1f;

        [Header("Suelo")]
        [Tooltip("Capas que cuentan como suelo. Vacío = capa \"Suelo\".")]
        [SerializeField] LayerMask groundLayers;
        [SerializeField] float groundCheckHeight = 0.1f;

        [Header("Trepar, agua y empujar")]
        [SerializeField] float climbSpeed = 3.5f;
        [Range(0.1f, 1f)] [SerializeField] float waterSpeedMultiplier = 0.6f;
        [Range(0f, 1f)] [SerializeField] float waterGravityMultiplier = 0.3f;
        [SerializeField] float swimStroke = 6f;
        [SerializeField] float maxSinkSpeed = 3f;
        [Tooltip("Velocidad al empujar una caja (más lenta que andar).")]
        [SerializeField] float pushSpeed = 3f;
        [SerializeField] float interactRadius = 1.2f;

        // ---------- Estado (solo lectura desde fuera) ----------
        public bool IsControlled { get; set; } = true;
        public IInputSource Input { get; set; } = KeyboardInput.Instance;
        public bool IsGrounded { get; private set; }
        public bool IsClimbing => climb != null;
        public bool IsSwinging => swing != null;
        public bool IsPushing => pushTimer > 0f;
        public bool IsInWater { get; private set; }
        public bool IsHeadUnderwater { get; private set; }
        public bool IsGravityFlipped => gravityDir < 0;
        public float GravitySign => gravityDir;
        public float Facing { get; private set; } = 1f;
        public float SpeedMultiplier { get; set; } = 1f;
        public float JumpSpeed => jumpSpeed;
        public bool CanAct => IsControlled && !DialogueManager.IsCinematicPlaying && Time.timeScale > 0f;
        public Vector3 RespawnPoint { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public Vector2 Center => col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;
        public IInteractable NearbyInteractable { get; private set; }
        public CharacterHealth Health { get; private set; }
        public float FallLimitBottom { get; set; } = -30f;
        public float FallLimitTop { get; set; } = 60f;

        public event Action<PlayerCharacter> Respawned;
        public event Action<PlayerCharacter> CheckpointReached;

        // ---------- Privado ----------
        protected SpriteRenderer Sprite { get; private set; }
        Collider2D col;
        BoxCollider2D box;
        Vector2 boxOffset;
        float baseGravity = 3f;
        int gravityDir = 1;
        float moveInput, climbInput, coyoteTimer, bufferTimer, pushTimer;
        Climbable climb;
        SwingRope swing;
        Collider2D groundUnder;

        // Botones leídos una sola vez por frame.
        struct Frame { public float h, v; public bool jump, jumpUp, jumpButton, interact, ability, secondary; }
        Frame input;
        protected bool AbilityPressedThisFrame => input.ability;
        // Para la cuerda-péndulo (que se agarra al personaje y le lee los controles).
        public float VerticalInput => input.v;
        public bool JumpButtonThisFrame => input.jumpButton;
        protected bool SecondaryPressedThisFrame => input.secondary;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Health = GetComponent<CharacterHealth>();
            Sprite = GetComponentInChildren<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            box = col as BoxCollider2D;
            if (box != null) boxOffset = box.offset;
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (Mathf.Abs(Body.gravityScale) > 0.01f) baseGravity = Mathf.Abs(Body.gravityScale);
            Body.gravityScale = baseGravity;
            if (groundLayers.value == 0) groundLayers = LayerMask.GetMask("Suelo");
            RespawnPoint = transform.position;
        }

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        // ================================================================================================
        // Bucle principal. [Patrón "método plantilla"]: el orden es siempre el mismo y las subclases solo
        // rellenan sus partes (UpdateAbilities, OnEnterWater...).
        // ================================================================================================
        void Update()
        {
            ReadInput();
            CheckGround();
            coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - Time.deltaTime;
            UpdateWater();

            bool canAct = CanAct;
            if (moveInput != 0f) Facing = Mathf.Sign(moveInput);

            if (IsSwinging) bufferTimer = 0f;   // la cuerda-péndulo se encarga del salto para soltarse
            else if (IsClimbing) { UpdateClimb(canAct); bufferTimer = 0f; }
            else
            {
                bool jump = input.jump;
                if (canAct && gravityDir > 0 && (input.v > 0.5f || (input.v < -0.5f && !IsGrounded)))
                {
                    Climbable found = Climbable.Find(Center, this);
                    if (found != null) { StartClimb(found); jump = false; }
                }
                if (!IsClimbing) UpdateJump(canAct, jump);
            }

            UpdateInteraction(canAct);
            UpdateAbilities(canAct);   // ← cada personaje la suya
            ApplyGravityScale();
            if (Sprite != null && moveInput != 0f) Sprite.flipX = moveInput < 0f;
            CheckBounds();
            pushTimer -= Time.deltaTime;
        }

        void ReadInput()
        {
            input = default;
            if (!CanAct || Input == null) { moveInput = 0f; return; }
            input.h = Input.Horizontal;
            input.v = Input.Vertical;
            input.jump = Input.JumpPressed;
            input.jumpUp = Input.JumpReleased;
            input.jumpButton = input.jump && Input.JumpButtonPressed;
            input.interact = Input.InteractPressed;
            input.ability = Input.AbilityPressed;
            input.secondary = Input.SecondaryPressed;
            moveInput = input.h;
        }

        void FixedUpdate()
        {
            Vector2 v = Body.linearVelocity;
            if (IsClimbing)
            {
                v.x = (climb.CenterX - Body.position.x) * 15f;
                v.y = climbInput * climbSpeed;
                Body.linearVelocity = v;
                return;
            }
            if (IsSwinging)
            {
                Body.AddForce(new Vector2(moveInput * swing.PushForce, 0f));
                return;
            }

            float speed = moveSpeed * SpeedMultiplier * (IsInWater ? waterSpeedMultiplier : 1f);
            if (moveInput != 0f && IsGrounded && TryPushAhead(moveInput)) speed = Mathf.Min(speed, pushSpeed);
            v.x = Mathf.MoveTowards(v.x, moveInput * speed, acceleration * Time.fixedDeltaTime);
            if (IsInWater && v.y * gravityDir < -maxSinkSpeed) v.y = -maxSinkSpeed * gravityDir;
            Body.linearVelocity = v;
        }

        // ---------- Huecos para las subclases (virtual) ----------

        // Habilidades propias (Volt: gravedad y linterna). Por defecto no hace nada.
        protected virtual void UpdateAbilities(bool canAct) { }

        // Al entrar en el agua. Por defecto, chapuzón.
        protected virtual void OnEnterWater() => GameAudio.Play("chapuzon");

        // Ha andado contra algo que se puede empujar pero no tiene fuerza (Volt con una caja).
        protected virtual void OnPushBlocked(IPushable pushable) { }

        // Por defecto se puede trepar si se tiene la habilidad que pide cada tipo de escalada.
        public virtual bool CanClimb(ClimbKind kind) =>
            kind == ClimbKind.Escalera ? Has(CharacterAbility.TreparEscaleras) : Has(CharacterAbility.TreparCuerdas);

        // ---------- Suelo, agua y límites ----------

        void CheckGround()
        {
            Bounds b = col != null ? col.bounds : new Bounds(transform.position, Vector3.one * 0.9f);
            Vector2 point = new Vector2(b.center.x, gravityDir > 0 ? b.min.y : b.max.y);
            Vector2 size = new Vector2(Mathf.Max(0.05f, b.size.x * 0.9f), groundCheckHeight);
            groundUnder = Physics2D.OverlapBox(point, size, 0f, groundLayers);
            // Subiendo a través de una plataforma de madera no se cuenta como suelo.
            IsGrounded = groundUnder != null && groundUnder != col && Body.linearVelocity.y * gravityDir <= 0.5f;
        }

        void UpdateWater()
        {
            float halfHeight = col != null ? col.bounds.extents.y : 0.5f;
            bool was = IsInWater;
            IsInWater = WaterZone.Contains(Center);
            IsHeadUnderwater = WaterZone.Contains(Center + Vector2.up * (halfHeight * 0.8f * gravityDir));
            if (IsInWater && !was) OnEnterWater();
        }

        bool CanSwim => Has(CharacterAbility.Nadar);

        // Caerse del mapa o meterse en el mundo del otro personaje (p. ej. por un pozo): vuelve al checkpoint.
        void CheckBounds()
        {
            float y = transform.position.y;
            bool outOfWorld = WorldMap.HasTwoWorlds && WorldMap.WorldAt(y) != HomeWorld;
            if (y < FallLimitBottom || y > FallLimitTop || outOfWorld) Health.Damage(DamageType.Caida);
        }

        void ApplyGravityScale()
        {
            if (IsClimbing) { Body.gravityScale = 0f; return; }
            float multiplier = IsInWater && CanSwim && gravityDir > 0 ? waterGravityMultiplier : 1f;
            Body.gravityScale = baseGravity * gravityDir * multiplier;
        }

        // ---------- Saltar y nadar ----------

        void UpdateJump(bool canAct, bool jumpPressed)
        {
            if (jumpPressed) bufferTimer = jumpBuffer;
            else bufferTimer -= Time.deltaTime;

            Vector2 v = Body.linearVelocity;

            // Bajar de una plataforma de madera: abajo + saltar.
            if (bufferTimer > 0f && input.v < -0.5f && IsGrounded && groundUnder != null && groundUnder.GetComponent<PlatformEffector2D>() != null)
            {
                StartCoroutine(DropThrough(groundUnder));
                bufferTimer = 0f;
                return;
            }

            if (IsInWater && CanSwim && gravityDir > 0)
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
                float speed = IsInWater ? jumpSpeed * 0.8f : jumpSpeed;   // quien no nada, salta menos en el agua
                v.y = speed * gravityDir;
                bufferTimer = 0f;
                coyoteTimer = 0f;
                GameAudio.Play("salto");
            }
            if (canAct && input.jumpUp && v.y * gravityDir > 0f && !IsInWater)
                v.y *= jumpCutMultiplier;
            Body.linearVelocity = v;
        }

        IEnumerator DropThrough(Collider2D platform)
        {
            if (col == null) yield break;
            Physics2D.IgnoreCollision(col, platform, true);
            yield return new WaitForSeconds(0.35f);
            if (col != null && platform != null) Physics2D.IgnoreCollision(col, platform, false);
        }

        // ---------- Empujar ----------

        // Mira si delante hay algo empujable. Si el personaje tiene fuerza, lo empuja; si no, avisa.
        bool TryPushAhead(float direction)
        {
            if (col == null) return false;
            Bounds b = col.bounds;
            Vector2 origin = new Vector2(b.center.x, b.center.y);
            RaycastHit2D hit = Physics2D.BoxCast(origin, new Vector2(0.1f, b.size.y * 0.6f), 0f, new Vector2(direction, 0f), b.extents.x + 0.08f, groundLayers);
            if (hit.collider == null) return false;
            var pushable = hit.collider.GetComponentInParent<IPushable>();
            if (pushable == null) return false;
            if (!Has(CharacterAbility.FuerzaManual)) { OnPushBlocked(pushable); return false; }
            if (!pushable.TryPush(this, direction)) return false;
            pushTimer = 0.15f;
            return true;
        }

        // ---------- Interactuar (palancas, terminales) ----------

        void UpdateInteraction(bool canAct)
        {
            NearbyInteractable = WorldMap.NearestInteractable(Center, interactRadius);
            if (!canAct || !input.interact || NearbyInteractable == null) return;
            // [Polimorfismo] Da igual si es una palanca o un terminal: cada uno sabe qué hacer.
            if (NearbyInteractable.CanBeUsedBy(this)) NearbyInteractable.Interact(this);
            else GameAudio.Play("dano", 0.3f);
        }

        // ---------- Trepar ----------

        void StartClimb(Climbable target)
        {
            climb = target;
            Body.linearVelocity = Vector2.zero;
        }

        void StopClimb()
        {
            climb = null;
            climbInput = 0f;
        }

        void UpdateClimb(bool canAct)
        {
            climbInput = input.v;
            Vector2 center = Center;
            if (canAct && input.jumpButton)
            {
                StopClimb();
                Body.linearVelocity = new Vector2(moveInput * moveSpeed, jumpSpeed * 0.8f);
                return;
            }
            if (!climb.Contains(center))
            {
                bool wentUp = center.y > climb.Area.yMax - 0.01f;   // arriba del todo: empujón hacia la plataforma
                StopClimb();
                if (wentUp) Body.linearVelocity = new Vector2(Facing * 1.5f, 7f);
                return;
            }
            if (IsGrounded && input.v < -0.1f) StopClimb();
        }

        // ---------- Cuerda-péndulo (SwingRope la llama) ----------

        public void AttachSwing(SwingRope rope)
        {
            StopClimb();
            swing = rope;
        }

        public void DetachSwing() => swing = null;

        // ---------- Gravedad ----------

        public void FlipGravity()
        {
            gravityDir = -gravityDir;
            // El collider está un poco desplazado hacia los pies: se refleja para que los pies sigan tocando el techo.
            if (box != null) box.offset = new Vector2(boxOffset.x, boxOffset.y * gravityDir);
            if (Sprite != null) Sprite.flipY = gravityDir < 0;
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
            RespawnPoint = position;
            if (DialogueManager.Instance != null) DialogueManager.Instance.ResetFalls(DisplayName);
            CheckpointReached?.Invoke(this);
        }

        public void Respawn()
        {
            ResetMotion();
            transform.position = RespawnPoint;
            Body.position = RespawnPoint;
            if (DialogueManager.Instance != null) DialogueManager.Instance.NotifyFall(DisplayName);
            Respawned?.Invoke(this);
        }

        // Lo usa el LevelBuilder al colocar al personaje en un nivel nuevo.
        public void PlaceAt(Vector3 position)
        {
            ResetMotion();
            SpeedMultiplier = 1f;
            transform.position = position;
            Body.position = position;
            SetCheckpoint(position);
        }

        void ResetMotion()
        {
            StopClimb();
            if (swing != null) swing.Release(false);
            ForceNormalGravity();
            Body.linearVelocity = Vector2.zero;
        }

        void OnDrawGizmosSelected()
        {
            var c = GetComponent<Collider2D>();
            if (c == null) return;
            Bounds b = c.bounds;
            float g = Application.isPlaying ? gravityDir : 1f;
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(new Vector3(b.center.x, g > 0 ? b.min.y : b.max.y, 0f), new Vector3(b.size.x * 0.9f, groundCheckHeight, 0f));
            Gizmos.color = new Color(1f, 0.83f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(Center, interactRadius);
        }
    }
}
