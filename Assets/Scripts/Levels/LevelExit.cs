using UnityEngine;

namespace EntreDosMons.Levels
{
    // Salida del nivel ('E' en el mapa). Cada mundo tiene la suya: Nil sale por la del bosque y Volt por la de la cueva.
    // El nivel solo se acaba cuando han llegado LOS DOS (se avanza juntos o no se avanza).
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        LevelBuilder builder;

        public void Init(LevelBuilder owner)
        {
            builder = owner;
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player != null && builder != null) builder.ReportArrived(player);
        }
    }
}
