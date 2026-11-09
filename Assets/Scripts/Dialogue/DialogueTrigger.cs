using UnityEngine;

namespace EntreDosMons.Dialogue
{
    // Zona que dispara un diálogo cuando entra Nil, Volt o cualquiera de los dos.
    // Ponedlo en un GameObject con un BoxCollider2D marcado como Is Trigger.
    [RequireComponent(typeof(Collider2D))]
    public class DialogueTrigger : MonoBehaviour
    {
        public enum Who { Cualquiera, Nil, Volt }
        public enum Activation { AlEntrar, AlInteractuar }

        [SerializeField] string dialogueId;
        [SerializeField] Who character = Who.Cualquiera;
        [SerializeField] Activation activation = Activation.AlEntrar;

        [Header("Tags de los personajes")]
        [SerializeField] string nilTag = "Nil";
        [SerializeField] string voltTag = "Volt";

        [Header("Interactuar")]
        [Tooltip("Icono «E» que aparece encima del objeto (opcional).")]
        [SerializeField] GameObject interactPrompt;

        [Tooltip("Desactiva la zona cuando el diálogo ya no puede volver a sonar.")]
        [SerializeField] bool disableWhenDone = true;

        public string DialogueId => dialogueId;

        // Para crear la zona desde código (LevelBuilder).
        public void Setup(string id, Who who, Activation mode)
        {
            dialogueId = id;
            character = who;
            activation = mode;
        }

        bool inside;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Start()
        {
            if (interactPrompt != null) interactPrompt.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!Matches(other)) return;
            if (activation == Activation.AlEntrar) TryPlay();
            else
            {
                inside = true;
                RefreshPrompt();
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!Matches(other)) return;
            inside = false;
            RefreshPrompt();
        }

        void Update()
        {
            if (activation != Activation.AlInteractuar || !inside) return;
            if (DialogueManager.IsCinematicPlaying) return;
            if (DialogueInput.InteractPressed()) TryPlay();
        }

        void TryPlay()
        {
            var dm = DialogueManager.Instance;
            if (dm == null) return;
            dm.Play(dialogueId);
            if (disableWhenDone && dm.WasPlayed(dialogueId))
            {
                inside = false;
                RefreshPrompt();
                enabled = false;
                GetComponent<Collider2D>().enabled = false;
            }
        }

        void RefreshPrompt()
        {
            if (interactPrompt == null) return;
            var dm = DialogueManager.Instance;
            interactPrompt.SetActive(inside && dm != null && dm.CanPlay(dialogueId));
        }

        bool Matches(Collider2D other)
        {
            var go = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            bool isNil = go.tag == nilTag;
            bool isVolt = go.tag == voltTag;
            switch (character)
            {
                case Who.Nil: return isNil;
                case Who.Volt: return isVolt;
                default: return isNil || isVolt;
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;
            Gizmos.color = character == Who.Volt ? new Color(1f, 0.83f, 0.3f, 0.35f)
                         : character == Who.Nil ? new Color(0.93f, 0.42f, 0.18f, 0.35f)
                         : new Color(0.3f, 0.54f, 0.65f, 0.35f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            UnityEditor.Handles.Label(col.bounds.center + Vector3.up * col.bounds.extents.y, dialogueId);
        }
#endif
    }
}
