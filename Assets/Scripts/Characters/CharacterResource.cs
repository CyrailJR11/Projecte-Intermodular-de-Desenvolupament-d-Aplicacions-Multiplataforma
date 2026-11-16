using UnityEngine;

namespace EntreDosMons
{
    // [POO · Abstracción + Polimorfismo] Un "recurso" de un personaje que se gasta y se recupera y que se ve en
    // el HUD como una barra: la respiración de Nil y la batería de Volt. El HUD no sabe cuál es cuál:
    // recorre los CharacterResource del personaje y pinta cada uno con lo que la subclase le dice.
    [RequireComponent(typeof(PlayerCharacter))]
    public abstract class CharacterResource : MonoBehaviour
    {
        protected PlayerCharacter Character { get; private set; }

        protected virtual void Awake() => Character = GetComponent<PlayerCharacter>();

        public abstract float Normalized { get; }   // 0..1
        public abstract string HudLabel { get; }    // texto corto al lado de la barra ("AIRE", "57%")
        public abstract Color HudColor { get; }
        public virtual bool ShowInHud => true;       // la respiración solo se ve cuando falta aire
        public abstract void Refill();               // nivel nuevo o reinicio: al máximo
    }
}
