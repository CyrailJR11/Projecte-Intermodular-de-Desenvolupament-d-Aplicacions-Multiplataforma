using UnityEngine;

namespace EntreDosMons.UI
{
    // HUD básico creado por código (uGUI, Canvas en Screen Space - Overlay escalado a 640×360 como la cámara):
    //   · Arriba a la izquierda, Nil: vidas (corazones) y respiración (solo aparece cuando le falta aire).
    //   · Arriba a la derecha, Volt: vidas, batería en % (verde/amarillo/naranja, parpadea al final) y si la linterna va encendida.
    //   · El nombre del personaje que se controla se ve resaltado.
    //   · En el centro, avisos cortos: "-1 vida", "Volt: no tengo fuerza para esta caja"...
    //   · Abajo, qué se puede hacer con lo que hay al lado ("E · Girar la válvula") o por qué no.
    // [POO · Polimorfismo] La barra de cada personaje es su CharacterResource (respiración o batería): el HUD
    // no sabe cuál es, solo le pide Normalized, HudLabel y HudColor.
    // Los iconos se dibujan en código (pixel art) para no depender de más archivos.
    public class GameHUD : MonoBehaviour
    {
        PlayerCharacter nil;
        PlayerCharacter volt;

        static GameHUD instance;

        // Aviso corto en el centro de la pantalla desde cualquier script (p. ej. Volt al chocar con una caja).
        public static void Say(string text, float seconds = 2f)
        {
            if (instance != null) instance.ShowMessage(text, seconds);
            else Debug.Log(text);
        }

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
        UnityEngine.UI.Text prompt;
        float messageTimer;

        void Awake()
        {
            font = GameFonts.Pixel;   // Press Start 2P (tipografía oficial)
            heartSprite = MakeSprite(".##.##.", "#######", "#######", ".#####.", "..###..", "...#...");
            bubbleSprite = MakeSprite(".###.", "#..##", "#...#", "#...#", ".###.");
            boltSprite = MakeSprite("..##", ".##.", "####", ".##.", "##..");
            whiteSprite = MakeSprite("#");
            BuildCanvas();
        }

        void OnEnable()
        {
            instance = this;
            CharacterHealth.LifeLost += OnLifeLost;
            CharacterHealth.OutOfLives += OnOutOfLives;
        }

        void OnDisable()
        {
            if (instance == this) instance = null;
            CharacterHealth.LifeLost -= OnLifeLost;
            CharacterHealth.OutOfLives -= OnOutOfLives;
        }

        void OnLifeLost(CharacterHealth who, DamageType type)
        {
            if (who.Lives <= 0) return;
            string cause = type == DamageType.Pinchos ? "pinchos" : type == DamageType.Ahogado ? "sin aire"
                : type == DamageType.Cortocircuito ? "cortocircuito" : "fuera de su mundo";
            ShowMessage($"{who.Character.DisplayName}: -1 vida ({cause})", 1.5f);
        }

        void OnOutOfLives(CharacterHealth who) => ShowMessage($"{who.Character.DisplayName} se ha quedado sin vidas. Reiniciando el nivel...", 2.5f);

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

            message = MakeText("Aviso", transform, 14, TextAnchor.MiddleCenter);
            message.font = GameFonts.Bold;   // frase larga: Atkinson Hyperlegible
            RectTransform mr = message.rectTransform;
            mr.anchorMin = mr.anchorMax = mr.pivot = new Vector2(0.5f, 1f);
            mr.anchoredPosition = new Vector2(0f, -64f);
            mr.sizeDelta = new Vector2(420f, 24f);
            message.enabled = false;

            prompt = MakeText("Accion", transform, 12, TextAnchor.MiddleCenter);
            prompt.font = GameFonts.Bold;
            RectTransform pr = prompt.rectTransform;
            pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0f);
            pr.anchoredPosition = new Vector2(0f, 10f);
            pr.sizeDelta = new Vector2(420f, 20f);
            prompt.enabled = false;
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

            p.label = MakeText("Nombre", root, 8, align);
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

            UpdateBar(nilPanel, nil);
            UpdateBar(voltPanel, volt);
            UpdatePrompt();

            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                if (messageTimer <= 0f) message.enabled = false;
            }
        }

        // [Polimorfismo] Respiración o batería: el HUD solo pregunta al CharacterResource.
        void UpdateBar(CharacterPanel panel, PlayerCharacter player)
        {
            CharacterResource resource = player != null ? player.GetComponent<CharacterResource>() : null;
            bool show = resource != null && resource.ShowInHud;
            panel.bar.SetActive(show);
            if (!show) return;
            float n = resource.Normalized;
            panel.barFill.fillAmount = n;
            Color c = resource.HudColor;
            if (n < 0.1f && Mathf.FloorToInt(Time.unscaledTime * 4f) % 2 == 0) c = Dim;
            panel.barFill.color = c;
            string extra = player is VoltCharacter v && v.Flashlight != null && v.Flashlight.IsLit ? " LUZ" : "";
            panel.barText.text = resource.HudLabel + extra;
        }

        // Qué se puede hacer con lo que hay al lado del personaje que se controla.
        void UpdatePrompt()
        {
            PlayerCharacter current = nil != null && nil.IsControlled ? nil : volt != null && volt.IsControlled ? volt : null;
            IInteractable near = current != null ? current.NearbyInteractable : null;
            prompt.enabled = near != null;
            if (near != null)
            {
                prompt.text = near.GetPrompt(current);
                prompt.color = near.CanBeUsedBy(current) ? Ink : Orange;
            }
        }

        void UpdatePanel(CharacterPanel panel, PlayerCharacter player)
        {
            bool active = player != null && player.IsControlled;
            panel.label.color = active ? Highlight : Ink;
            var health = player != null ? player.Health : null;
            int max = health != null ? Mathf.Min(health.MaxLives, panel.hearts.Length) : 0;
            for (int i = 0; i < panel.hearts.Length; i++)
            {
                panel.hearts[i].enabled = i < max;
                panel.hearts[i].color = health != null && i < health.Lives ? HeartColor : Dim;
            }
        }

        void FindCharacters()
        {
            foreach (PlayerCharacter p in PlayerCharacter.All)
            {
                if (p is NilCharacter) nil = p;
                else if (p is VoltCharacter) volt = p;
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
