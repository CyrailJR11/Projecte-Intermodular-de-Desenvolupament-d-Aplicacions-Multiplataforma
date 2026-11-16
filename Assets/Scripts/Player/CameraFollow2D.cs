using UnityEngine;

namespace EntreDosMons
{
    // Cámara que sigue al personaje con suavizado. Ponlo en la Main Camera.
    // Si marcas "Use Bounds", la cámara no se sale del rectángulo min/max (los bordes del nivel).
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public Vector2 offset = new Vector2(0f, 1f);
        public float smoothTime = 0.15f;

        public bool useBounds;
        public Vector2 min = new Vector2(-10f, -5f);
        public Vector2 max = new Vector2(70f, 10f);

        Vector3 velocity;
        Vector3 smoothPosition;   // posición suave (sin redondear); la cámara se coloca redondeada a la rejilla de píxeles
        bool started;

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 goal = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            if (useBounds)
            {
                goal.x = Mathf.Clamp(goal.x, min.x, max.x);
                goal.y = Mathf.Clamp(goal.y, min.y, max.y);
            }
            if (!started) { smoothPosition = transform.position; started = true; }
            smoothPosition = Vector3.SmoothDamp(smoothPosition, goal, ref velocity, smoothTime);
            // Redondeada a píxeles del juego (1/32 de casilla): el pixel art no "tiembla" al moverse la cámara.
            float ppu = ScreenSetup.PixelsPerUnit;
            transform.position = new Vector3(Mathf.Round(smoothPosition.x * ppu) / ppu,
                                             Mathf.Round(smoothPosition.y * ppu) / ppu, smoothPosition.z);
        }
    }
}
