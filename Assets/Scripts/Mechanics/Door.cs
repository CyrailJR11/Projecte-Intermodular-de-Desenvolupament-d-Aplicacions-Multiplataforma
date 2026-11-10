using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Compuerta metálica ('D' en el mapa, una columna de casillas). Es sólida hasta que una palanca la abre:
    // entonces la hoja sube y deja pasar.
    public class Door : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public float speed = 6f;

        BoxCollider2D solid;
        Transform leaf;
        float height;

        // Lo llama el LevelBuilder: crea una pieza de sprite por casilla y el collider de toda la columna.
        public void Init(Sprite sprite, int cells, Material material, int sortingOrder)
        {
            height = cells;
            solid = gameObject.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1f, cells);
            solid.offset = new Vector2(0f, (cells - 1) / 2f);

            leaf = new GameObject("Hoja").transform;
            leaf.SetParent(transform, false);
            for (int i = 0; i < cells; i++)
            {
                var piece = new GameObject("Pieza " + i).AddComponent<SpriteRenderer>();
                piece.transform.SetParent(leaf, false);
                piece.transform.localPosition = new Vector3(0f, i, 0f);
                piece.sprite = sprite;
                piece.sortingOrder = sortingOrder;
                if (material != null) piece.sharedMaterial = material;
            }
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (solid != null) solid.enabled = !open;
        }

        public void Toggle() => SetOpen(!IsOpen);

        void Update()
        {
            if (leaf == null) return;
            float target = IsOpen ? height - 0.25f : 0f;
            Vector3 p = leaf.localPosition;
            p.y = Mathf.MoveTowards(p.y, target, speed * Time.deltaTime);
            leaf.localPosition = p;
        }
    }
}
