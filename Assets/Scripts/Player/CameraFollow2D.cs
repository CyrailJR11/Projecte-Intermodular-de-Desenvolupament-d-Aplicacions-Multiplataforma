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

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 goal = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            if (useBounds)
            {
                goal.x = Mathf.Clamp(goal.x, min.x, max.x);
                goal.y = Mathf.Clamp(goal.y, min.y, max.y);
            }
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }
    }
}
