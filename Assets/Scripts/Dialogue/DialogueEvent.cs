using UnityEngine;

namespace EntreDosMons.Dialogue
{
    // Para diálogos que no salen de una zona sino de algo que pasa en el juego:
    // un generador que se enciende, la semilla que se recoge, la cúpula que se abre...
    // Conectad Fire() al UnityEvent de vuestro objeto (botón "+" en el Inspector),
    // o llamad desde código a DialogueManager.Instance.Play("id").
    public class DialogueEvent : MonoBehaviour
    {
        [SerializeField] string dialogueId;
        [Tooltip("Flag opcional que se activa al disparar (además de las del JSON).")]
        [SerializeField] string setFlag;

        public string DialogueId => dialogueId;

        public void Fire()
        {
            var dm = DialogueManager.Instance;
            if (dm == null) return;
            if (!string.IsNullOrEmpty(setFlag)) dm.SetFlag(setFlag);
            dm.Play(dialogueId);
        }

        // Para usar el mismo componente con varios diálogos desde UnityEvents.
        public void FireId(string id)
        {
            if (DialogueManager.Instance != null) DialogueManager.Instance.Play(id);
        }

        public void SetFlag(string flag)
        {
            if (DialogueManager.Instance != null) DialogueManager.Instance.SetFlag(flag);
        }
    }
}
