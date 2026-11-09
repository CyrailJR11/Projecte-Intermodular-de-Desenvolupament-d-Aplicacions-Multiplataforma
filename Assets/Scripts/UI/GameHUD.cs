using UnityEngine;

namespace EntreDosMons.UI
{
    // HUD básico creado por código (uGUI, Canvas en Screen Space - Overlay escalado a 640×360 como la cámara):
    //   · Arriba a la izquierda, Nil: vidas (corazones) y respiración (solo aparece cuando le falta aire).
    //   · Arriba a la derecha, Volt: vidas, batería en % (verde/amarillo/naranja, parpadea al final) y si la linterna va encendida.
    //   · El nombre del personaje que se controla se ve resaltado.
    //   · En el centro, avisos cortos: "-1 vida", "Sin vidas: se reinicia el nivel".
    // Los iconos se dibujan en código (pixel art) para no depender de más archivos.
    public class GameHUD : MonoBehaviour
    {
        public PlayerController2D nil;
        public PlayerController2D volt;

        static readonly Color Ink = new Color32(0xF5, 0xF0, 0xD8, 0xFF);
        static readonly Color Dim = new Color32(0x4B, 0x63, 0x74, 0xFF);
        static readonly Color Panel = new Color32(0x07, 0x18, 0x27, 0xB0);
        static readonly Color Highlight = new Color32(0xFF, 0xD3, 0x4E, 0xFF);
        static readonly Color HeartColor = new Color32(0xEE, 0x6C, 0x2F, 0xFF);
        static readonly Color Water = new Color32(0x4B, 0x89, 0xA6, 0xFF);
        static readonly Color Green = new Color32(0x62, 0xB5, 0x6B, 0xFF);
        static readonly Color Orange = new Color32(0xEE, 0x6C, 0x2F, 0xFF);

        class CharacterPanel
        {
            public UnityEngine.UI.Text label;
            public UnityEngine.UI.Image[] hearts;
            public GameObject bar;
            public UnityEngine.UI.Image barFill;
            public UnityEngine.UI.Image barIcon;
            public UnityEngine.UI.Text barText;
        }

        Font font;
        Sprite heartSprite, whiteSprite, bubbleSprite, boltSprite;
        CharacterPanel nilPanel, voltPanel;
        UnityEngine.UI.Text message;
        float messageTimer;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            heartSprite = MakeSprite(".##.##.", "#######", "#######", ".#####.", "..###..", "...#...");
            bubbleSprite = MakeSprite(".###.", "#..##", "#...#", "#...#", ".###.");
            boltSprite = MakeSprite("..##", ".##.", "####", ".##.", "##..");
            whiteSprite = MakeSprite("#");
            BuildCanvas();
        }

        void OnEnable()
        {
            PlayerHealth.LifeLost += OnLifeLost;
            PlayerHealth.OutOfLives += OnOutOfLives;
        }

        void OnDisable()
        {
            PlayerHealth.LifeLost -= OnLifeLost;
            PlayerHealth.OutOfLives -= OnOutOfLives;
        }

        void OnLifeLost(PlayerHealth who, DamageType type)
        {
            if (who.Lives <= 0) return;
            string cause = type == DamageType.Pinchos ? "pinchos" : type == DamageType.Ahogado ? "sin aire"
                : type == DamageType.Cortocircuito ? "cortocircuito" : "caída";
            ShowMessage($"{who.Player.characterName}: -1 vida ({cause})", 1.5f);
        }

        void OnOutOfLives(PlayerHealth who) => ShowMessage($"{who.Player.characterName} se ha quedado sin vidas. Reiniciando el nivel...", 2.5f);

        public void ShowMessage(string text, float seconds)
        {
            message.text = text;
            message.enabled = true;
            messageTimer = seconds;
        }

        // ---------- Construcción del Canvas ----------

        void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;   // por debajo del panel de diálogos si ese usa un orden mayor
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.matchWidthOrHeight = 0.5f;

            nilPanel = BuildPanel("Nil", new Vector2(0f, 1f), new Vector2(8f, -8f), "NIL", bubbleSprite, Water);
            voltPanel = BuildPanel("Volt", new Vector2(1f, 1f), new Vector2(-8f, -8f), "VOLT", boltSprite, Green);

            message = MakeText("Aviso", transform, 12, TextAnchor.MiddleCenter);
            RectTransform mr = message.rectTransform;
            mr.anchorMin = mr.anchorMax = mr.pivot = new Vector2(0.5f, 1f);
            mr.anchoredPosition = new Vector2(0f, -64f);
            mr.sizeDelta = new Vector2(420f, 24f);
            message.enabled = false;
        }

        CharacterPanel BuildPanel(string name, Vector2 corner, Vector2 position, string title, Sprite icon, Color barColor)
        {
            var p = new CharacterPanel();
            RectTransform root = MakeRect(name, transform, corner, position, new Vector2(132f, 44f));
            MakeImage("Fondo", root, whiteSprite, Panel, Vector2.zero, Vector2.zero, Vector2.one);

            bool right = corner.x > 0.5f;
            TextAnchor align = right ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            float side = right ? -6f : 6f;
            Vector2 anchor = new Vector2(right ? 1f : 0f, 1f);

            p.label = MakeText("Nombre", root, 10, align);
            Place(p.label.rectTransform, anchor, new Vector2(side, -3f), new Vector2(120f, 14f));
            p.label.text = title;

            p.hearts = new UnityEngine.UI.Image[5];
            for (int i = 0; i < p.hearts.Length; i++)
            {
                float x = side + (right ? -1f : 1f) * i * 17f;
                p.hearts[i] = MakeImage("Vida " + (i + 1), root, heartSprite, HeartColor, anchor, new Vector2(x, -17f), new Vector2(14f, 12f));
            }

            p.bar = new GameObject("Barra", typeof(RectTransform));
            var barRect = (RectTransform)p.bar.transform;
            barRect.SetParent(root, false);
            Place(barRect, anchor, new Vector2(side, -31f), new Vector2(120f, 10f));
            Vector2 a = new Vector2(right ? 1f : 0f, 0.5f);
            float dir = right ? -1f : 1f;
            p.barIcon = MakeImage("Icono", barRect, icon, Ink, a, Vector2.zero, new Vector2(8f, 10f));
            MakeImage("Hueco", barRect, whiteSprite, Dim, a, new Vector2(dir * 12f, 0f), new Vector2(70f, 6f));
            p.barFill = MakeImage("Relleno", barRect, whiteSprite, barColor, a, new Vector2(dir * 12f, 0f), new Vector2(70f, 6f));
            p.barFill.type = UnityEngine.UI.Image.Type.Filled;
            p.barFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            p.barFill.fillOrigin = right ? 1 : 0;
            p.barText = MakeText("Valor", barRect, 8, right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            Place(p.barText.rectTransform, a, new Vector2(dir * 86f, 0f), new Vector2(40f, 10f));
            return p;
        }

        // ---------- Actualización ----------

        void LateUpdate()
        {
            if (nil == null || volt == null) FindCharacters();

            UpdatePanel(nilPanel, nil);
            UpdatePanel(voltPanel, volt);

            // Respiración de Nil: solo cuando le falta aire.
            var breath = nil != null ? nil.GetComponent<PlayerBreath>() : null;
            bool showBreath = breath != null && (!breath.IsFull || nil.IsHeadUnderwater);
            nilPanel.bar.SetActive(showBreath);
            if (showBreath)
            {
                nilPanel.barFill.fillAmount = breath.Normalized;
                nilPanel.barFill.color = breath.Normalized < 0.3f ? Orange : Water;
                nilPanel.barText.text = "AIRE";
            }

            // Batería de Volt: siempre visible.
            var battery = volt != null ? volt.GetComponent<VoltBattery>() : null;
            voltPanel.bar.SetActive(battery != null);
            if (battery != null)
            {
                float n = battery.Normalized;
                voltPanel.barFill.fillAmount = n;
                Color c = n > 0.5f ? Green : n > 0.2f ? Highlight : Orange;
                if (n < 0.1f && Mathf.FloorToInt(Time.unscaledTime * 4f) % 2 == 0) c = Dim;
                voltPanel.barFill.color = c;
                var flashlight = volt.GetComponent<VoltFlashlight>();
                string light = flashlight != null && flashlight.IsLit ? " LUZ" : "";
                voltPanel.barText.text = (battery.IsCharging ? "+" : "") + Mathf.CeilToInt(battery.Charge) + "%" + light;
                voltPanel.barIcon.color = battery.IsCharging ? Highlight : Ink;
            }

            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                if (messageTimer <= 0f) message.enabled = false;
            }
        }

        void UpdatePanel(CharacterPanel panel, PlayerController2D player)
        {
            bool active = player != null && player.IsControlled;
            panel.label.color = active ? Highlight : Ink;
            var health = player != null ? player.GetComponent<PlayerHealth>() : null;
            int max = health != null ? Mathf.Min(health.maxLives, panel.hearts.Length) : 0;
            for (int i = 0; i < panel.hearts.Length; i++)
            {
                panel.hearts[i].enabled = i < max;
                panel.hearts[i].color = health != null && i < health.Lives ? HeartColor : Dim;
            }
        }

        void FindCharacters()
        {
            foreach (PlayerController2D p in PlayerController2D.All)
            {
                if (nil == null && p.characterName == "Nil") nil = p;
                else if (volt == null && p.characterName == "Volt") volt = p;
            }
        }

        // ---------- Utilidades ----------

        static RectTransform MakeRect(string name, Transform parent, Vector2 corner, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            Place(r, corner, position, size);
            return r;
        }

        // Ancla y pivot en el mismo punto (esquina o borde) y después la posición.
        static void Place(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = position;
            r.sizeDelta = size;
        }

        static UnityEngine.UI.Image MakeImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            if (size == Vector2.one && anchor == Vector2.zero)
            {
                // Estirado a todo el padre (fondo del panel).
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = r.offsetMax = Vector2.zero;
            }
            else Place(r, anchor, position, size);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        UnityEngine.UI.Text MakeText(string name, Transform parent, int size, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<UnityEngine.UI.Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = Ink;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = go.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color32(0x07, 0x18, 0x27, 0xFF);
            outline.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        // Sprite pixel art a partir de filas de texto ('#' = píxel lleno), en blanco para teñirlo con Image.color.
        static Sprite MakeSprite(params string[] rows)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, h - 1 - y, rows[y][x] == '#' ? Color.white : Color.clear);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
