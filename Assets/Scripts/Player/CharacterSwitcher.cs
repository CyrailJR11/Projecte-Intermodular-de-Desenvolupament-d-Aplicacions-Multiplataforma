using UnityEngine;
using EntreDosMons.Dialogue;

namespace EntreDosMons
{
    // Cambia entre Nil y Volt con Q: solo el elegido se mueve y la cámara lo sigue.
    public class CharacterSwitcher : MonoBehaviour
    {
        public PlayerController2D[] characters;
        public CameraFollow2D cameraFollow;
        [Tooltip("Personaje con el que empieza la partida (0 = el primero de la lista).")]
        public int startIndex;

        int current;
        public PlayerController2D Current => characters[current];

        void Start() => Select(startIndex);

        void Update()
        {
            if (characters.Length < 2 || DialogueManager.IsCinematicPlaying) return;
            if (!GameInput.SwitchPressed()) return;

            Select((current + 1) % characters.Length);
            if (DialogueManager.Instance != null) DialogueManager.Instance.PlayBark("bark_cambio", 0.15f);
        }

        public void Select(int index)
        {
            current = Mathf.Clamp(index, 0, characters.Length - 1);
            for (int i = 0; i < characters.Length; i++)
                characters[i].IsControlled = i == current;
            if (cameraFollow != null) cameraFollow.target = characters[current].transform;
        }
    }
}
