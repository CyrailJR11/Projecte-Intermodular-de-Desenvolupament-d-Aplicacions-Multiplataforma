using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Montacargas de la estación ('H' = posición de abajo, ':' = recorrido). Sin corriente está abajo; cuando su
    // canal se activa (Volt conecta el terminal) sube hasta arriba llevando a quien esté encima.
    // Es un Rigidbody2D cinemático: lo mueve el código (MovePosition) y empuja a lo que tenga encima.
    [RequireComponent(typeof(Rigidbody2D))]
    public class Lift : SignalReceiver
    {
        [SerializeField] float speed = 2f;
        [SerializeField] Sprite sprite;
        [SerializeField] int sortingOrder = 1;

        public bool IsUp { get; private set; }
        Rigidbody2D body;
        Vector2 bottom, top;

        // El objeto está en el centro de la plataforma, en su posición de abajo.
        public void SetSprite(Sprite platform) => sprite = platform;

        public void Build(int width, int travel)
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            bottom = transform.position;
            top = bottom + Vector2.up * travel;

            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 0.5f);
            col.offset = new Vector2(0f, 0.25f);

            for (int i = 0; i < width; i++)
            {
                var piece = new GameObject("Pieza " + i).AddComponent<SpriteRenderer>();
                piece.transform.SetParent(transform, false);
                piece.transform.localPosition = new Vector3(i - (width - 1) / 2f, 0f, 0f);
                piece.sprite = sprite;
                piece.sortingOrder = sortingOrder;
            }
        }

        public override void OnSignal(bool active)
        {
            IsUp = active;
            GameAudio.Play("gravedad", 0.5f);
        }

        void FixedUpdate()
        {
            if (body == null) return;
            Vector2 target = IsUp ? top : bottom;
            if (body.position == target) return;
            body.MovePosition(Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime));
        }
    }
}
