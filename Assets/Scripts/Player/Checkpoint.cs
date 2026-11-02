using UnityEngine;

namespace EntreDosMons
{
    // Punto de control: el personaje que lo toque reaparecerá aquí si se cae.
    // Necesita un Collider2D con "Is Trigger" marcado.
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController2D>();
            if (player != null) player.SetCheckpoint(transform.position);
        }
    }
}
