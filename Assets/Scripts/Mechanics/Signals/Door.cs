using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Compuerta ('D'). Vertical: una columna de 'D' que se esconde subiendo hacia el techo.
    // Horizontal (trampilla): una o varias 'D' en el suelo que se meten en la pared de al lado; lo que haya
    // encima (una caja, un personaje) cae al hueco.
    public class Door : SignalReceiver
    {
        [SerializeField] float speed = 6f;
        [Tooltip("Sprite de cada casilla de la compuerta vertical y de la trampilla.")]
        [SerializeField] Sprite verticalSprite;
        [SerializeField] Sprite horizontalSprite;
        [Tooltip("Detrás del suelo (0): al abrirse la hoja se esconde dentro de la roca.")]
        [SerializeField] int sortingOrder = -1;

        public bool IsOpen { get; private set; }
        public bool IsHorizontal { get; private set; }

        BoxCollider2D solid;
        Transform leaf;
        int cells;

        // Lo llama el LevelBuilder: una pieza de sprite por casilla y un collider para toda la compuerta.
        // El objeto está en la casilla de abajo (vertical) o de la izquierda (horizontal).
        public void SetSprites(Sprite vertical, Sprite horizontal)
        {
            verticalSprite = vertical;
            horizontalSprite = horizontal;
        }

        public void Build(int cellCount, bool horizontal)
        {
            Sprite sprite = horizontal && horizontalSprite != null ? horizontalSprite : verticalSprite;
            cells = Mathf.Max(1, cellCount);
            IsHorizontal = horizontal;
            solid = GetComponent<BoxCollider2D>();
            if (solid == null) solid = gameObject.AddComponent<BoxCollider2D>();
            Vector2 along = horizontal ? Vector2.right : Vector2.up;
            solid.size = horizontal ? new Vector2(cells, 1f) : new Vector2(1f, cells);
            solid.offset = along * ((cells - 1) / 2f);

            leaf = new GameObject("Hoja").transform;
            leaf.SetParent(transform, false);
            for (int i = 0; i < cells; i++)
            {
                var piece = new GameObject("Pieza " + i).AddComponent<SpriteRenderer>();
                piece.transform.SetParent(leaf, false);
                piece.transform.localPosition = (Vector3)(along * i);
                piece.sprite = sprite;
                piece.sortingOrder = sortingOrder;
            }
        }

        public override void OnSignal(bool active) => SetOpen(active);

        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            if (solid != null) solid.enabled = !open;
            GameAudio.Play("palanca", 0.6f);
            if (open) WakeBodiesOnTop();
        }

        // Lo que estaba quieto encima de la trampilla (dormido para las físicas) tiene que caer.
        void WakeBodiesOnTop()
        {
            if (solid == null) return;
            Bounds b = solid.bounds;
            Collider2D[] hits = Physics2D.OverlapBoxAll(b.center + Vector3.up * 0.6f, new Vector2(b.size.x + 0.2f, b.size.y + 0.4f), 0f);
            foreach (Collider2D c in hits)
                if (c.attachedRigidbody != null) c.attachedRigidbody.WakeUp();
        }

        void Update()
        {
            if (leaf == null) return;
            Vector3 target = IsOpen ? (IsHorizontal ? Vector3.left * cells : Vector3.up * (cells - 0.25f)) : Vector3.zero;
            leaf.localPosition = Vector3.MoveTowards(leaf.localPosition, target, speed * Time.deltaTime);
        }
    }
}
