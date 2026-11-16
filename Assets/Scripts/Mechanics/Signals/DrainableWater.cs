using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EntreDosMons.Mechanics
{
    // Agua con desagüe (casillas de agua con cable). Cuando su canal se activa (Nil gira la válvula del río)
    // el agua baja y la galería queda seca para Volt; si se cierra la válvula, se vuelve a llenar.
    public class DrainableWater : SignalReceiver
    {
        [SerializeField] float drainSpeed = 2f;   // casillas por segundo

        public bool IsDrained { get; private set; }

        readonly List<WaterZone> zones = new List<WaterZone>();
        readonly List<Rect> fullAreas = new List<Rect>();
        readonly List<(Vector3Int cell, TileBase tile)> tiles = new List<(Vector3Int, TileBase)>();
        Tilemap tilemap;
        float level = 1f;   // 1 = lleno, 0 = vacío
        float minY, maxY;

        public void Build(IEnumerable<WaterZone> waterZones, Tilemap map, IEnumerable<Vector3Int> cells)
        {
            tilemap = map;
            minY = float.MaxValue;
            maxY = float.MinValue;
            foreach (WaterZone z in waterZones)
            {
                zones.Add(z);
                fullAreas.Add(z.Area);
                minY = Mathf.Min(minY, z.Area.yMin);
                maxY = Mathf.Max(maxY, z.Area.yMax);
            }
            foreach (Vector3Int c in cells) tiles.Add((c, map.GetTile(c)));
        }

        public override void OnSignal(bool active)
        {
            IsDrained = active;
            GameAudio.Play("chapuzon", 0.6f);
        }

        void Update()
        {
            float target = IsDrained ? 0f : 1f;
            if (Mathf.Approximately(level, target) || zones.Count == 0) return;
            float height = Mathf.Max(0.01f, maxY - minY);
            level = Mathf.MoveTowards(level, target, drainSpeed / height * Time.deltaTime);
            float surface = Mathf.Lerp(minY, maxY, level);

            // Cada zona de agua se recorta por arriba hasta la superficie actual.
            for (int i = 0; i < zones.Count; i++)
            {
                Rect full = fullAreas[i];
                float top = Mathf.Min(full.yMax, surface);
                zones[i].Area = new Rect(full.x, full.y, full.width, Mathf.Max(0f, top - full.y));
                zones[i].enabled = top > full.y + 0.01f;
            }
            // Los tiles por encima de la superficie desaparecen; los de debajo vuelven a estar.
            if (tilemap == null) return;
            foreach (var t in tiles)
            {
                float cellY = tilemap.CellToWorld(t.cell).y;
                tilemap.SetTile(t.cell, cellY + 0.5f < surface ? t.tile : null);
            }
        }
    }
}
