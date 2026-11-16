using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Caja de madera ('X'). Pesa demasiado para Volt: solo Nil (FuerzaManual) la puede empujar.
    // Arreglo del empuje: antes era un cuerpo físico normal que se quedaba enganchado por el rozamiento y se
    // empujaba "a tirones". Ahora:
    //   · Quieta, tiene la X congelada (nadie la mueve sin querer, ni Volt chocando con ella) pero cae normal.
    //   · Cuando Nil la empuja, PlayerCharacter llama a TryPush() y la caja se mueve a velocidad constante.
    //   · Al soltarla se recoloca en el centro de la casilla, así siempre encaja en placas, huecos y escalones.
    // [POO · Interfaz] Implementa IPushable: el personaje no sabe que es una caja.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PushableBox : MonoBehaviour, IPushable
    {
        [SerializeField] float pushSpeed = 3f;
        [SerializeField] float snapSpeed = 4f;
        [SerializeField] float resetBelowY = -5f;
        [SerializeField] LayerMask solidLayers;

        public bool IsBeingPushed => pushTimer > 0f;

        Rigidbody2D body;
        Collider2D col;
        Vector3 start;
        float pushTimer;
        bool snapping;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            if (solidLayers.value == 0) solidLayers = LayerMask.GetMask("Suelo");
            start = transform.position;
            Freeze(true);
        }

        void Freeze(bool freezeX) =>
            body.constraints = freezeX ? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation
                                       : RigidbodyConstraints2D.FreezeRotation;

        public bool TryPush(PlayerCharacter by, float direction)
        {
            if (by == null || !by.Has(CharacterAbility.FuerzaManual)) return false;
            if (!IsResting() || IsBlocked(direction)) return false;
            pushTimer = 0.1f;
            snapping = false;
            Freeze(false);
            body.linearVelocity = new Vector2(Mathf.Sign(direction) * pushSpeed, body.linearVelocity.y);
            return true;
        }

        // Solo se empuja apoyada en algo (no en el aire).
        bool IsResting()
        {
            Bounds b = col.bounds;
            return Hit(new Vector2(b.center.x, b.min.y - 0.05f), new Vector2(b.size.x * 0.8f, 0.08f));
        }

        // ¿Hay pared u otra caja justo delante?
        bool IsBlocked(float direction)
        {
            Bounds b = col.bounds;
            float x = direction > 0f ? b.max.x + 0.06f : b.min.x - 0.06f;
            return Hit(new Vector2(x, b.center.y), new Vector2(0.08f, b.size.y * 0.8f));
        }

        bool Hit(Vector2 center, Vector2 size)
        {
            foreach (Collider2D c in Physics2D.OverlapBoxAll(center, size, 0f, solidLayers))
                if (c != col && !c.isTrigger) return true;
            return false;
        }

        void FixedUpdate()
        {
            if (pushTimer > 0f)
            {
                pushTimer -= Time.fixedDeltaTime;
                if (pushTimer <= 0f) snapping = true;
                return;
            }
            if (!snapping) return;

            // Recolocar en el centro de la casilla y volver a congelar la X.
            float targetX = Mathf.Floor(body.position.x) + 0.5f;
            float x = Mathf.MoveTowards(body.position.x, targetX, snapSpeed * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            body.position = new Vector2(x, body.position.y);
            if (Mathf.Abs(x - targetX) < 0.001f)
            {
                snapping = false;
                Freeze(true);
            }
        }

        public void ResetPosition()
        {
            pushTimer = 0f;
            snapping = false;
            body.position = start;
            transform.position = start;
            body.linearVelocity = Vector2.zero;
            Freeze(true);
        }

        void Update()
        {
            if (transform.position.y < resetBelowY) ResetPosition();
        }
    }
}
