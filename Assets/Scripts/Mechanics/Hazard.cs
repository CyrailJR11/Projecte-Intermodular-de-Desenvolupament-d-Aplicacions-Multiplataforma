using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Pinchos ('^' en el mapa): quien los toque pierde una vida y vuelve al último punto de control.
    // [POO · Interfaz] No pregunta "¿es Nil o Volt?": daña a cualquier cosa que sea IDamageable.
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [SerializeField] DamageType type = DamageType.Pinchos;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other) => Hit(other);
        void OnTriggerStay2D(Collider2D other) => Hit(other);

        void Hit(Collider2D other)
        {
            var target = other.GetComponentInParent<IDamageable>();
            target?.Damage(type);
        }
    }
}
