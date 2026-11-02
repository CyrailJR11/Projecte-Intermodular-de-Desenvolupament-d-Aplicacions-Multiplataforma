using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EntreDosMons.Dialogue
{
    // Dibuja los diálogos: panel inferior (radio, terminal, cuaderno) y, si se configura,
    // un bocadillo encima del personaje para las frases en voz alta (canal "local").
    public class DialogueUI : MonoBehaviour
    {
        [Serializable] public class Portrait { public string speaker; public string emotion = "neutral"; public Sprite sprite; }
        [Serializable] public class Anchor { public string speaker; public Transform target; }
        [Serializable] public class Voice { public string speaker; public AudioClip blip; [Range(0.5f, 2f)] public float pitch = 1f; }
        [Serializable] public class Sfx { public string name; public AudioClip clip; }

        [Header("Panel inferior")]
        [SerializeField] CanvasGroup panel;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Image portraitImage;
        [SerializeField] GameObject advanceIcon;   // flechita "pulsa para seguir" (solo cinemáticas)

        [Header("Marcos por canal (se activa uno)")]
        [SerializeField] GameObject radioFrame;
        [SerializeField] GameObject localFrame;
        [SerializeField] GameObject terminalFrame;
        [SerializeField] GameObject notebookFrame;

        [Header("Bocadillo sobre el personaje (opcional)")]
        [SerializeField] RectTransform bubble;
        [SerializeField] TMP_Text bubbleText;
        [SerializeField] Vector3 bubbleWorldOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField] Anchor[] anchors;          // NIL → transform de Nil, VOLT → transform de Volt, AVI_ROC...

        [Header("Retratos")]
        [SerializeField] Portrait[] portraits;

        [Header("Texto")]
        [SerializeField] float charsPerSecond = 45f;
        [SerializeField] float terminalCharsPerSecond = 80f;

        [Header("Interferencia de la radio")]
        [SerializeField] AudioSource staticLoop;    // ruido blanco en loop, volumen 0 al empezar
        [SerializeField] float maxStaticVolume = 0.35f;
        [SerializeField] float maxJitterPixels = 2.5f;

        [Header("Sonido")]
        [SerializeField] AudioSource sfxSource;
        [SerializeField] Voice[] voices;
        [SerializeField] Sfx[] sfx;

        DialogueManager dm;
        TMP_Text activeText;
        Transform bubbleTarget;
        Coroutine typing;
        bool isTyping;
        float interference;
        Vector2 bodyBasePos;
        Canvas canvas;
        int lineFrame;

        void Start()
        {
            dm = DialogueManager.Instance;
            if (dm == null) { Debug.LogError("[Diálogos] Falta un DialogueManager en la escena."); enabled = false; return; }
            dm.DialogueEnded += OnEnded;
            dm.LineShown += OnLine;
            canvas = GetComponentInParent<Canvas>();
            if (bodyText != null) bodyBasePos = bodyText.rectTransform.anchoredPosition;
            HideAll();
        }

        void OnDestroy()
        {
            if (dm == null) return;
            dm.DialogueEnded -= OnEnded;
            dm.LineShown -= OnLine;
        }

        void Update()
        {
            if (dm == null || !dm.IsPlaying) return;

            // Time.frameCount: la misma pulsación que abre el diálogo no salta la primera línea
            if (dm.Current.IsCinematic && Time.frameCount != lineFrame && DialogueInput.AdvancePressed())
            {
                if (isTyping) FinishTyping();
                else dm.Advance();
            }

            if (advanceIcon != null) advanceIcon.SetActive(dm.Current.IsCinematic && !isTyping);

            UpdateInterference();
            FollowBubbleTarget();
        }

        void OnEnded(DialogueEntry e)
        {
            if (typing != null) StopCoroutine(typing);
            isTyping = false;
            HideAll();
        }

        void OnLine(DialogueLine line, SpeakerInfo speaker, DialogueEntry entry)
        {
            lineFrame = Time.frameCount;
            interference = line.channel == "radio" ? Mathf.Clamp01(line.interference) : 0f;
            bubbleTarget = line.channel == "local" ? FindAnchor(line.speaker) : null;

            bool useBubble = bubbleTarget != null && bubble != null && bubbleText != null;
            if (panel != null) panel.alpha = useBubble ? 0f : 1f;
            if (bubble != null) bubble.gameObject.SetActive(useBubble);

            if (!useBubble)
            {
                SetFrame(line.channel);
                if (nameText != null)
                {
                    nameText.text = speaker != null ? speaker.name : line.speaker;
                    if (speaker != null && ColorUtility.TryParseHtmlString(speaker.color, out var c)) nameText.color = c;
                }
                if (portraitImage != null)
                {
                    var sprite = FindPortrait(line.speaker, line.emotion);
                    portraitImage.sprite = sprite;
                    portraitImage.enabled = sprite != null;
                }
            }

            activeText = useBubble ? bubbleText : bodyText;
            if (!string.IsNullOrEmpty(line.sfx)) PlaySfx(line.sfx);

            if (typing != null) StopCoroutine(typing);
            if (activeText == null) { Debug.LogWarning("[Diálogos] DialogueUI sin texto asignado."); return; }
            float cps = line.channel == "terminal" ? terminalCharsPerSecond : charsPerSecond;
            typing = StartCoroutine(Type(line, cps));
        }

        IEnumerator Type(DialogueLine line, float cps)
        {
            isTyping = true;
            activeText.text = line.text;
            activeText.maxVisibleCharacters = 0;
            activeText.ForceMeshUpdate();
            int total = activeText.textInfo.characterCount;
            var voice = FindVoice(line.speaker);

            float shown = 0f;
            int last = 0;
            while (last < total)
            {
                shown += Time.deltaTime * cps;
                int now = Mathf.Min(total, Mathf.FloorToInt(shown));
                if (now != last)
                {
                    activeText.maxVisibleCharacters = now;
                    if (voice != null && voice.blip != null && sfxSource != null && now % 3 == 0)
                    {
                        sfxSource.pitch = voice.pitch * UnityEngine.Random.Range(0.95f, 1.05f);
                        sfxSource.PlayOneShot(voice.blip, 0.5f);
                    }
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
            if (activeText != null) activeText.maxVisibleCharacters = int.MaxValue;
        }

        // Parpadeo y temblor del texto + ruido de fondo según la interferencia de la línea.
        void UpdateInterference()
        {
            if (staticLoop != null) staticLoop.volume = Mathf.MoveTowards(staticLoop.volume, interference * maxStaticVolume, Time.deltaTime);
            if (bodyText == null) return;

            if (interference <= 0f)
            {
                bodyText.rectTransform.anchoredPosition = bodyBasePos;
                bodyText.alpha = 1f;
                return;
            }
            bool glitch = UnityEngine.Random.value < interference * 0.25f;
            bodyText.rectTransform.anchoredPosition = bodyBasePos + (glitch ? UnityEngine.Random.insideUnitCircle * maxJitterPixels * interference : Vector2.zero);
            bodyText.alpha = glitch ? 1f - interference * 0.6f : 1f;
        }

        void FollowBubbleTarget()
        {
            if (bubbleTarget == null || bubble == null || canvas == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 screen = cam.WorldToScreenPoint(bubbleTarget.position + bubbleWorldOffset);
            var parent = bubble.parent as RectTransform;
            var uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCam, out var local))
                bubble.anchoredPosition = local;
        }

        void SetFrame(string channel)
        {
            if (radioFrame != null) radioFrame.SetActive(channel == "radio");
            if (localFrame != null) localFrame.SetActive(channel == "local");
            if (terminalFrame != null) terminalFrame.SetActive(channel == "terminal");
            if (notebookFrame != null) notebookFrame.SetActive(channel == "cuaderno");
        }

        void HideAll()
        {
            if (panel != null) panel.alpha = 0f;
            if (bubble != null) bubble.gameObject.SetActive(false);
            if (advanceIcon != null) advanceIcon.SetActive(false);
            interference = 0f;
            bubbleTarget = null;
            if (bodyText != null) { bodyText.text = ""; bodyText.alpha = 1f; bodyText.rectTransform.anchoredPosition = bodyBasePos; }
        }

        Transform FindAnchor(string speaker)
        {
            if (anchors == null) return null;
            foreach (var a in anchors) if (a.speaker == speaker && a.target != null) return a.target;
            return null;
        }

        Sprite FindPortrait(string speaker, string emotion)
        {
            if (portraits == null) return null;
            Sprite fallback = null;
            foreach (var p in portraits)
            {
                if (p.speaker != speaker) continue;
                if (p.emotion == emotion) return p.sprite;
                if (p.emotion == "neutral") fallback = p.sprite;
            }
            return fallback;
        }

        Voice FindVoice(string speaker)
        {
            if (voices == null) return null;
            foreach (var v in voices) if (v.speaker == speaker) return v;
            return null;
        }

        void PlaySfx(string name)
        {
            if (sfx == null || sfxSource == null) return;
            foreach (var s in sfx)
                if (s.name == name && s.clip != null) { sfxSource.pitch = 1f; sfxSource.PlayOneShot(s.clip); return; }
        }
    }
}
