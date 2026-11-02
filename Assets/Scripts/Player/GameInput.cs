using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EntreDosMons
{
    // Teclas del juego. Funciona con el Input System nuevo y con el antiguo (igual que DialogueInput).
    //   Mover: A/D o flechas · Saltar: Espacio, W o flecha arriba · Cambiar personaje: Q · Invertir gravedad (Volt): F
    //   Mando: stick izquierdo, A/Cruz salta, Y/Triángulo cambia, RB/R1 invierte la gravedad.
    public static class GameInput
    {
        public static float Horizontal()
        {
#if ENABLE_INPUT_SYSTEM
            float x = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                float stick = gp.leftStick.x.ReadValue();
                if (Mathf.Abs(stick) > 0.3f) x = Mathf.Sign(stick);
                if (gp.dpad.left.isPressed) x = -1f;
                if (gp.dpad.right.isPressed) x = 1f;
            }
            return x;
#else
            return Input.GetAxisRaw("Horizontal");
#endif
        }

        public static bool JumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame))
                || (gp != null && gp.buttonSouth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#endif
        }

        public static bool JumpReleased()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && (kb.spaceKey.wasReleasedThisFrame || kb.wKey.wasReleasedThisFrame || kb.upArrowKey.wasReleasedThisFrame))
                || (gp != null && gp.buttonSouth.wasReleasedThisFrame);
#else
            return Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
#endif
        }

        public static bool SwitchPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && kb.qKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Q);
#endif
        }

        public static bool FlipGravityPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && kb.fKey.wasPressedThisFrame) || (gp != null && gp.rightShoulder.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.F);
#endif
        }
    }
}
