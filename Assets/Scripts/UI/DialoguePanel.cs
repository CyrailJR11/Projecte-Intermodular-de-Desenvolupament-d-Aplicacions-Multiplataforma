using System.Collections;
using EntreDosMons.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace EntreDosMons.UI
{
    // Panel de diálogos creado por código (uGUI), para que los diálogos se vean sin montar nada a mano.
    // Lo crea el LevelBuilder al darle a Play si en la escena no hay un DialogueUI (el de TextMeshPro).
    //   · Abajo: nombre del personaje (Press Start 2P, en su color) y la frase (Atkinson Hyperlegible) letra a letra.
    //   · Radio: el texto tiembla y parpadea según la interferencia de la línea.
    //   · Cinemáticas: flechita parpadeante; E / Espacio / Enter / clic completan la frase o pasan a la siguiente.
    public class DialoguePanel : MonoBehaviour
    {
        static readonly Color Ink = new Color32(0xF5, 0xF0, 0xD8, 0xFF);
        static readonly Color Back = new Color32(0x07, 0x18, 0x27, 0xE6);
        static readonly Color Border = new Color32(0x4B, 0x89, 0xA6, 0xFF);
        static readonly Color TerminalInk = new Color32(0x62, 0xB5, 0x6B, 0xFF);
        static readonly Color NotebookBack = new Color32(0x10, 0x2B, 0x3A, 0xF0);

        [SerializeField] float charsPerSecond = 45f;
        [SerializeField] float terminalCharsPerSecond = 80f;

        DialogueManager dm;
        CanvasGroup group;
        Image background;
        Text nameText, bodyText, channelText, arrow;
        Vector2 bodyBasePos;
        Coroutine typing;
        bool isTyping;
        string fullText = "";
        float interference;
        int lineFrame;

        void Awake() => BuildCanvas();

        void Start()
        {
            dm = DialogueManager.Instance;
            if (dm == null) { Debug.LogError("[Diálogos] Falta un DialogueManager en la escena."); enabled = false; return; }
            dm.LineShown += OnLine;
            dm.DialogueEnded += OnEnded;
            Hide();
        }

        void OnDestroy()
        {
            if (dm == null) return;
            dm.LineShown -= OnLine;
            dm.DialogueEnded -= OnEnded;
        }

        void Update()
        {
            if (dm == null || !dm.IsPlaying) return;

            bool cinematic = dm.Current.IsCinematic;
            if (cinematic && Time.frameCount != lineFrame && DialogueInput.AdvancePressed())
            {
                if (isTyping) FinishTyping();
                else dm.Advance();
            }
            arrow.enabled = cinematic && !isTyping && Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.5f;

            if (interference > 0f)
            {
                bool glitch = Random.value < interference * 0.25f;
                bodyText.rectTransform.anchoredPosition = bodyBasePos + (glitch ? Random.insideUnitCircle * 2.5f * interference : Vector2.zero);
                var c = bodyText.color; c.a = glitch ? 1f - interference * 0.6f : 1f; bodyText.color = c;
            }
        }

        void OnLine(DialogueLine line, SpeakerInfo speaker, DialogueEntry entry)
        {
            lineFrame = Time.frameCount;
            group.alpha = 1f;
            interference = line.channel == "radio" ? Mathf.Clamp01(line.interference) : 0f;
            bodyText.rectTransform.anchoredPosition = bodyBasePos;

            nameText.text = (speaker != null && !string.IsNullOrEmpty(speaker.name) ? speaker.name : line.speaker).ToUpperInvariant();
            nameText.color = speaker != null && ColorUtility.TryParseHtmlString(speaker.color, out var sc) ? sc : Ink;

            bool terminal = line.channel == "terminal";
            bool notebook = line.channel == "cuaderno";
            bodyText.color = terminal ? TerminalInk : Ink;
            background.color = notebook ? NotebookBack : Back;
            channelText.text = line.channel == "radio" ? "RADIO" : terminal ? "TERMINAL" : notebook ? "CUADERNO" : "";

            if (typing != null) StopCoroutine(typing);
            typing = StartCoroutine(Type(line.text ?? "", terminal ? terminalCharsPerSecond : charsPerSecond));
        }

        IEnumerator Type(string text, float cps)
        {
            isTyping = true;
            fullText = text;
            bodyText.text = "";
            float shown = 0f;
            int last = 0;
            while (last < text.Length)
            {
                shown += Time.deltaTime * cps;
                int now = Mathf.Min(text.Length, Mathf.FloorToInt(shown));
                if (now != last)
                {
                    bodyText.text = text.Substring(0, now);
                    last = now;
                }
                yield return null;
            }
            isTyping = false;
            typing = null;
        }

        void FinishTyping()
        {
            if (typing != null) StopCoroutine(typing);
            typing = null;
            isTyping = false;
            bodyText.text = fullText;
        }

        void OnEnded(DialogueEntry e)
        {
            if (typing != null) StopCoroutine(typing);
            typing = null;
            isTyping = false;
            Hide();
        }

        void Hide()
        {
            group.alpha = 0f;
            interference = 0f;
            bodyText.text = "";
            arrow.enabled = false;
            bodyText.rectTransform.anchoredPosition = bodyBasePos;
        }

        // ---------- Construcción del Canvas ----------

        void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;   // por encima del HUD (5)
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.matchWidthOrHeight = 0.5f;

            var white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);

            var root = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup));
            var r = (RectTransform)root.transform;
            r.SetParent(transform, false);
            r.anchorMin = new Vector2(0.5f, 0f);
            r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(0f, 8f);
            r.sizeDelta = new Vector2(560f, 76f);
            group = root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            Stretch(MakeImage("Borde", r, white, Border), 0f);
            background = Stretch(MakeImage("Fondo", r, white, Back), 2f);

            nameText = MakeText("Nombre", r, GameFonts.Pixel, 8, TextAnchor.UpperLeft);
            Anchor(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(12f, -10f), new Vector2(300f, 12f));

            channelText = MakeText("Canal", r, GameFonts.Pixel, 8, TextAnchor.UpperRight);
            channelText.color = Border;
            Anchor(channelText.rectTransform, new Vector2(1f, 1f), new Vector2(-12f, -10f), new Vector2(200f, 12f));

            bodyText = MakeText("Texto", r, GameFonts.Body, 14, TextAnchor.UpperLeft);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.lineSpacing = 1.05f;
            Anchor(bodyText.rectTransform, new Vector2(0f, 1f), new Vector2(12f, -26f), new Vector2(530f, 44f));
            bodyBasePos = bodyText.rectTransform.anchoredPosition;

            arrow = MakeText("Seguir", r, GameFonts.Pixel, 8, TextAnchor.LowerRight);
            arrow.text = "▼";   // Press Start 2P lo incluye
            Anchor(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(-10f, 8f), new Vector2(16f, 12f));
        }

        static Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Image Stretch(Image img, float inset)
        {
            var r = img.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(inset, inset);
            r.offsetMax = new Vector2(-inset, -inset);
            return img;
        }

        static void Anchor(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = position;
            r.sizeDelta = size;
        }

        static Text MakeText(string name, Transform parent, Font font, int size, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = Ink;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
