using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Fuente que se acciona con E estando cerca. Cada subclase dice QUIÉN puede usarla (RequiredAbility)
    // y QUÉ texto enseña; el resto (registrarse, el texto del HUD, encender/apagar) es común.
    // [POO · Interfaz] Implementa IInteractable, así el personaje la encuentra y la usa sin saber qué es.
    public abstract class InteractableSource : SignalSource, IInteractable
    {
        protected abstract CharacterAbility RequiredAbility { get; }
        protected abstract string ActionText(bool turnOn);    // "Abrir la válvula" / "Cerrar la válvula"
        protected abstract string DeniedText { get; }        // por qué el otro personaje no puede

        public Vector3 Position => transform.position;
        public bool CanBeUsedBy(PlayerCharacter who) => who != null && who.Has(RequiredAbility);
        public string GetPrompt(PlayerCharacter who) => CanBeUsedBy(who) ? "E · " + ActionText(!IsActive) : DeniedText;

        public virtual void Interact(PlayerCharacter who)
        {
            if (!CanBeUsedBy(who)) return;
            SetActive(!IsActive);
        }

        protected virtual void OnEnable() => WorldMap.Register(this);
        protected virtual void OnDisable() => WorldMap.Unregister(this);
    }
}
