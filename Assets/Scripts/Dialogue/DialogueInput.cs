using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EntreDosMons.Dialogue
{
    // Lectura de teclas del sistema de diálogo. Funciona con el Input System nuevo y con el antiguo.
    public static class DialogueInput
    {
        // Interactuar con objetos (cuaderno, terminal, Avi Roc...): E o botón X/Cuadrado.
        public static bool InteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && kb.eKey.wasPressedThisFrame)
                || (gp != null && gp.buttonWest.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        // Pasar de línea en las cinemáticas: E, Enter, Espacio, clic o botón A/Cruz.
        public static bool AdvancePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            var mouse = Mouse.current;
            return (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (gp != null && gp.buttonSouth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
#endif
        }
    }
}
