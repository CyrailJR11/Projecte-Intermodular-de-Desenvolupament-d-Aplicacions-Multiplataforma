using UnityEngine;
using EntreDosMons.Dialogue;

namespace EntreDosMons
{
    // Respiración de Nil: baja mientras tiene la cabeza bajo el agua y se recupera al salir.
    // Si llega a cero, Nil se ahoga: pierde una vida y vuelve al último punto de control.
    [RequireComponent(typeof(PlayerController2D))]
    public class PlayerBreath : MonoBehaviour
    {
        [Tooltip("Segundos que aguanta bajo el agua.")]
        public float maxBreath = 8f;
        [Tooltip("Cuántas veces más rápido se recupera que se gasta.")]
        public float refillMultiplier = 3f;

        public float Breath { get; private set; }
        public float Normalized => maxBreath > 0f ? Breath / maxBreath : 1f;
        public bool IsFull => Breath >= maxBreath - 0.001f;

        PlayerController2D player;
        PlayerHealth health;

        void Awake()
        {
            player = GetComponent<PlayerController2D>();
            Breath = maxBreath;
        }

        void OnEnable() { if (player != null) player.Respawned += Refill; }
        void OnDisable() { if (player != null) player.Respawned -= Refill; }

        void Refill(PlayerController2D _) => Breath = maxBreath;
        public void ResetBreath() => Breath = maxBreath;

        void Update()
        {
            if (DialogueManager.IsCinematicPlaying) return;
            if (player.IsHeadUnderwater) Breath -= Time.deltaTime;
            else Breath += Time.deltaTime * refillMultiplier;
            Breath = Mathf.Clamp(Breath, 0f, maxBreath);

            if (Breath <= 0f)
            {
                Breath = maxBreath;
                if (health == null) health = GetComponent<PlayerHealth>();
                if (health != null) health.Damage(DamageType.Ahogado);
                else player.Respawn();
            }
        }
    }
}
