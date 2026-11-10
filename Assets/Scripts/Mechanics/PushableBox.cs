using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Caja de madera ('X' en el mapa) que se puede empujar andando contra ella y usar como escalón.
    // Si se cae fuera del nivel vuelve a su sitio.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PushableBox : MonoBehaviour
    {
        public float resetBelowY = -5f;
        Vector3 start;
        Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            start = transform.position;
        }

        public void ResetPosition()
        {
            rb.position = start;
            transform.position = start;
            rb.linearVelocity = Vector2.zero;
        }

        void Update()
        {
            if (transform.position.y < resetBelowY) ResetPosition();
        }
    }
}
