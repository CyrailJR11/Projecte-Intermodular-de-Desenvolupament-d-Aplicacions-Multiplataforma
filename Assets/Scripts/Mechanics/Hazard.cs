using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Pinchos ('^' en el mapa): quien los toque pierde una vida y vuelve al último punto de control.
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        public DamageType type = DamageType.Pinchos;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other) => Hit(other);
        void OnTriggerStay2D(Collider2D other) => Hit(other);

        void Hit(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.Damage(type);
        }
    }
}
