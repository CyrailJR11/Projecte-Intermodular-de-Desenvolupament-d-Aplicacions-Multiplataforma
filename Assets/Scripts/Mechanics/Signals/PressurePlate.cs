using System.Collections.Generic;
using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Placa de presión ('o'). Está activa mientras haya peso encima: un personaje o una caja.
    // Una caja la deja pulsada para siempre (y libera al personaje para hacer otra cosa).
    [RequireComponent(typeof(Collider2D))]
    public class PressurePlate : SignalSource
    {
        [SerializeField] Transform plateVisual;
        [SerializeField] float pressedDepth = 0.12f;

        readonly HashSet<Rigidbody2D> weights = new HashSet<Rigidbody2D>();
        Vector3 visualStart;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (plateVisual == null && transform.childCount > 0) plateVisual = transform.GetChild(0);
            if (plateVisual != null) visualStart = plateVisual.localPosition;
        }

        static bool HasWeight(Rigidbody2D body) =>
            body != null && (body.GetComponent<PlayerCharacter>() != null || body.GetComponent<PushableBox>() != null);

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!HasWeight(other.attachedRigidbody)) return;
            weights.Add(other.attachedRigidbody);
            UpdateState();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.attachedRigidbody == null) return;
            weights.Remove(other.attachedRigidbody);
            UpdateState();
        }

        void FixedUpdate()
        {
            // Por si algo desaparece encima (reaparecer, reiniciar).
            if (weights.RemoveWhere(b => b == null || !b.gameObject.activeInHierarchy) > 0) UpdateState();
        }

        void UpdateState() => SetActive(weights.Count > 0);

        protected override void OnActiveChanged(bool on)
        {
            if (on) GameAudio.Play("palanca", 0.5f);
            if (plateVisual != null) plateVisual.localPosition = visualStart + (on ? Vector3.down * pressedDepth : Vector3.zero);
        }
    }
}
