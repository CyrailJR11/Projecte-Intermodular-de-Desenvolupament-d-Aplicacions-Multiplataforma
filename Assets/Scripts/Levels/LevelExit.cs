using UnityEngine;

namespace EntreDosMons.Levels
{
    // Salida del nivel ('E' en el mapa): el primer personaje que la toca carga el siguiente nivel.
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        public LevelBuilder builder;
        bool used;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (used || builder == null || other.GetComponentInParent<PlayerController2D>() == null) return;
            used = true;
            builder.NextLevel();
        }
    }
}
