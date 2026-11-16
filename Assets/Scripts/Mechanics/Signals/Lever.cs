using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Válvula / palanca manual ('p'). Está oxidada: hace falta FUERZA para girarla → solo Nil.
    // [POO · Herencia] Solo dice lo que la hace distinta del terminal: quién la usa, los textos y cómo se ve.
    public class Lever : InteractableSource
    {
        [SerializeField] SpriteRenderer sprite;

        protected override CharacterAbility RequiredAbility => CharacterAbility.FuerzaManual;
        protected override string ActionText(bool turnOn) => turnOn ? "Girar la válvula" : "Cerrar la válvula";
        protected override string DeniedText => "Oxidada: hace falta la fuerza de Nil";

        void Awake() { if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>(); }

        protected override void OnActiveChanged(bool on)
        {
            GameAudio.Play("palanca");
            if (sprite != null) sprite.flipX = on;
        }
    }
}
