using System.Collections.Generic;
using UnityEngine;
using EntreDosMons.Dialogue;

namespace EntreDosMons
{
    // Cambia entre Nil y Volt con Q: solo el elegido se mueve y la cámara lo sigue.
    // Los personajes los crea el LevelBuilder a partir de sus prefabs y se los pasa con SetCharacters().
    public class CharacterSwitcher : MonoBehaviour
    {
        [SerializeField] CameraFollow2D cameraFollow;
        [Tooltip("Personaje con el que empieza la partida (0 = el primero de la lista).")]
        [SerializeField] int startIndex;

        readonly List<PlayerCharacter> characters = new List<PlayerCharacter>();
        int current;

        public IReadOnlyList<PlayerCharacter> Characters => characters;
        public PlayerCharacter Current => characters.Count > 0 ? characters[current] : null;
        public CameraFollow2D CameraFollow { get => cameraFollow; set => cameraFollow = value; }

        public void SetCharacters(IEnumerable<PlayerCharacter> list)
        {
            characters.Clear();
            characters.AddRange(list);
            Select(startIndex);
        }

        void Update()
        {
            if (characters.Count < 2 || DialogueManager.IsCinematicPlaying) return;
            if (!GameInput.SwitchPressed()) return;

            Select((current + 1) % characters.Count);
            if (DialogueManager.Instance != null) DialogueManager.Instance.PlayBark("bark_cambio", 0.15f);
        }

        public void Select(int index)
        {
            if (characters.Count == 0) return;
            current = Mathf.Clamp(index, 0, characters.Count - 1);
            for (int i = 0; i < characters.Count; i++)
                if (characters[i] != null) characters[i].IsControlled = i == current;
            if (cameraFollow != null && Current != null) cameraFollow.target = Current.transform;
        }

        public void Select(PlayerCharacter who)
        {
            int i = characters.IndexOf(who);
            if (i >= 0) Select(i);
        }
    }
}
