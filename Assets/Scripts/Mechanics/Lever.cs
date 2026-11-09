using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using EntreDosMons.Dialogue;

namespace EntreDosMons.Mechanics
{
    // Palanca ('p' en el mapa). El personaje controlado, junto a ella, pulsa E para accionarla:
    // abre o cierra las compuertas que tiene asignadas (el LevelBuilder le da las compuertas más cercanas).
    [RequireComponent(typeof(Collider2D))]
    public class Lever : MonoBehaviour
    {
        public List<Door> doors = new List<Door>();
        public bool IsOn { get; private set; }

        [Header("Tile que se gira al accionarla")]
        public Tilemap tilemap;
        public Vector3Int cell;

        readonly HashSet<PlayerController2D> inside = new HashSet<PlayerController2D>();

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponentInParent<PlayerController2D>();
            if (p != null) inside.Add(p);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var p = other.GetComponentInParent<PlayerController2D>();
            if (p != null) inside.Remove(p);
        }

        void Update()
        {
            if (DialogueManager.IsCinematicPlaying || !GameInput.InteractPressed()) return;
            foreach (PlayerController2D p in inside)
            {
                if (p != null && p.IsControlled)
                {
                    Toggle();
                    return;
                }
            }
        }

        public void Toggle()
        {
            IsOn = !IsOn;
            GameAudio.Play("palanca");
            if (tilemap != null)
                tilemap.SetTransformMatrix(cell, IsOn ? Matrix4x4.Scale(new Vector3(-1f, 1f, 1f)) : Matrix4x4.identity);
            foreach (Door d in doors)
                if (d != null) d.SetOpen(IsOn);
        }
    }
}
