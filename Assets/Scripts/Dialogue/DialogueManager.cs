using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EntreDosMons.Dialogue
{
    // Carga el guion (JSON), decide qué diálogo suena y en qué orden, y guarda qué se ha dicho ya.
    // No dibuja nada: DialogueUI escucha sus eventos. Poned uno solo en la escena (o en un prefab persistente).
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        // Los controladores de Nil y Volt pueden mirar esto para no moverse durante una cinemática.
        public static bool IsCinematicPlaying { get; private set; }

        [Header("Guion")]
        [Tooltip("Si se deja vacío, se carga Resources/Dialogues/dialogos_es.json")]
        [SerializeField] TextAsset dialogueJson;

        [Header("Ritmo en modo ambiente")]
        [SerializeField] float secondsPerChar = 0.045f;
        [SerializeField] float minLineSeconds = 1.8f;
        [SerializeField] float extraLineSeconds = 0.6f;
        [SerializeField] float pauseBetweenDialogues = 0.4f;

        [Header("Frases sueltas")]
        [Tooltip("Caídas seguidas en el mismo tramo antes de soltar un comentario.")]
        [SerializeField] int fallsBeforeBark = 4;
        [SerializeField] float barkCooldown = 20f;

        [Header("Guardado")]
        [Tooltip("Guarda lo ya dicho en PlayerPrefs. Desactivadlo si lo metéis en vuestro JSON de partida con GetSaveData/LoadSaveData.")]
        [SerializeField] bool usePlayerPrefs = true;
        [SerializeField] string playerPrefsKey = "EDM_Dialogos";

        public event Action<DialogueEntry> DialogueStarted;
        public event Action<DialogueEntry> DialogueEnded;
        public event Action<DialogueLine, SpeakerInfo, DialogueEntry> LineShown;
        public event Action<bool> CinematicStateChanged;
        public event Action<string> FlagSet;   // p. ej. "semilla_etapa_2" para cambiar el sprite de la semilla

        readonly Dictionary<string, DialogueEntry> entries = new Dictionary<string, DialogueEntry>();
        readonly Dictionary<string, SpeakerInfo> speakers = new Dictionary<string, SpeakerInfo>();
        readonly HashSet<string> played = new HashSet<string>();
        readonly HashSet<string> flags = new HashSet<string>();
        readonly Queue<DialogueEntry> queue = new Queue<DialogueEntry>();
        readonly Dictionary<string, int> fallCount = new Dictionary<string, int>();

        DialogueEntry current;
        Coroutine running;
        bool advanceRequested;
        float lastBarkTime = -999f;

        public DialogueEntry Current => current;
        public bool IsPlaying => current != null;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Load();
            if (usePlayerPrefs) LoadFromPlayerPrefs();
        }

        void OnDestroy()
        {
            if (Instance == this) { Instance = null; IsCinematicPlaying = false; }
        }

        void Load()
        {
            var asset = dialogueJson != null ? dialogueJson : Resources.Load<TextAsset>("Dialogues/dialogos_es");
            if (asset == null) { Debug.LogError("[Diálogos] No encuentro dialogos_es.json en Resources/Dialogues."); return; }

            var file = JsonUtility.FromJson<DialogueFile>(asset.text);
            entries.Clear();
            speakers.Clear();
            foreach (var s in file.speakers) speakers[s.id] = s;
            foreach (var d in file.dialogues)
            {
                if (entries.ContainsKey(d.id)) Debug.LogWarning($"[Diálogos] Id repetido: {d.id}");
                entries[d.id] = d;
            }
        }

        public bool HasDialogue(string id) => entries.ContainsKey(id);
        public bool WasPlayed(string id) => played.Contains(id);
        public SpeakerInfo GetSpeaker(string id) => speakers.TryGetValue(id, out var s) ? s : null;

        // ── Flags ─────────────────────────────────────────────

        public bool HasFlag(string flag) => flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || !flags.Add(flag)) return;
            FlagSet?.Invoke(flag);
            Save();
        }

        // ── Reproducir ────────────────────────────────────────

        // Devuelve true si el diálogo se ha puesto a sonar o en cola.
        public bool Play(string id)
        {
            if (!entries.TryGetValue(id, out var entry)) { Debug.LogWarning($"[Diálogos] No existe el diálogo '{id}'."); return false; }
            if (!CanPlay(entry)) return false;

            if (entry.once) played.Add(entry.id);   // se marca ya para que no entre dos veces en cola

            if (current == null) { StartEntry(entry); return true; }

            // Una cinemática corta un diálogo de ambiente; el resto espera su turno.
            if (entry.IsCinematic && !current.IsCinematic)
            {
                StopCurrent();
                StartEntry(entry);
            }
            else queue.Enqueue(entry);
            return true;
        }

        public bool CanPlay(string id) => entries.TryGetValue(id, out var e) && CanPlay(e);

        bool CanPlay(DialogueEntry entry)
        {
            if (entry.once && played.Contains(entry.id)) return false;
            if (entry.requires != null)
                foreach (var r in entry.requires)
                    if (!flags.Contains(r)) return false;
            return true;
        }

        // Frase suelta: solo suena si no hay nada más hablando y ha pasado el cooldown.
        public bool PlayBark(string id, float chance = 1f)
        {
            if (current != null || Time.time - lastBarkTime < barkCooldown) return false;
            if (UnityEngine.Random.value > chance) return false;
            if (!Play(id)) return false;
            lastBarkTime = Time.time;
            return true;
        }

        // Llamad a esto desde el respawn de cada personaje ("Nil" o "Volt").
        public void NotifyFall(string character)
        {
            fallCount.TryGetValue(character, out var n);
            fallCount[character] = ++n;
            if (n < fallsBeforeBark) return;
            if (PlayBark(character == "Volt" ? "bark_caidas_volt" : "bark_caidas_nil")) fallCount[character] = 0;
        }

        // Llamad a esto al llegar a un punto de control.
        public void ResetFalls(string character) => fallCount[character] = 0;

        // DialogueUI lo llama cuando el jugador pulsa para pasar de línea en una cinemática.
        public void Advance() => advanceRequested = true;

        // Saltar todo lo que está sonando (p. ej. desde el menú de pausa).
        public void SkipAll()
        {
            queue.Clear();
            StopCurrent();
        }

        void StartEntry(DialogueEntry entry)
        {
            current = entry;
            running = StartCoroutine(Run(entry));
        }

        void StopCurrent()
        {
            if (running != null) StopCoroutine(running);
            running = null;
            var e = current;
            current = null;
            SetCinematic(false);
            if (e == null) return;
            // Aunque se corte o se salte, sus flags se activan para no bloquear la historia.
            if (e.sets != null)
                foreach (var f in e.sets) SetFlag(f);
            DialogueEnded?.Invoke(e);
        }

        IEnumerator Run(DialogueEntry entry)
        {
            SetCinematic(entry.IsCinematic);
            DialogueStarted?.Invoke(entry);

            IEnumerable<DialogueLine> lines = entry.lines;
            if (entry.randomLine && entry.lines.Length > 0)
                lines = new[] { entry.lines[UnityEngine.Random.Range(0, entry.lines.Length)] };

            foreach (var line in lines)
            {
                advanceRequested = false;
                LineShown?.Invoke(line, GetSpeaker(line.speaker), entry);
                yield return null; // que la misma pulsación no pase dos líneas

                if (entry.IsCinematic)
                {
                    while (!advanceRequested) yield return null;
                }
                else
                {
                    float t = line.duration > 0f
                        ? line.duration
                        : Mathf.Max(minLineSeconds, line.text.Length * secondsPerChar + extraLineSeconds);
                    yield return new WaitForSeconds(t);
                }
            }

            if (entry.sets != null)
                foreach (var f in entry.sets) SetFlag(f);
            Save();

            running = null;
            current = null;
            SetCinematic(false);
            DialogueEnded?.Invoke(entry);

            if (queue.Count > 0)
            {
                yield return new WaitForSeconds(pauseBetweenDialogues);
                // Puede que algo nuevo haya empezado durante la pausa
                if (current == null && queue.Count > 0) StartEntry(queue.Dequeue());
            }
        }

        void SetCinematic(bool on)
        {
            if (IsCinematicPlaying == on) return;
            IsCinematicPlaying = on;
            CinematicStateChanged?.Invoke(on);
        }

        // ── Guardado ──────────────────────────────────────────

        public DialogueSaveData GetSaveData() => new DialogueSaveData
        {
            played = new List<string>(played).ToArray(),
            flags = new List<string>(flags).ToArray(),
        };

        public void LoadSaveData(DialogueSaveData data)
        {
            played.Clear();
            flags.Clear();
            if (data == null) return;
            if (data.played != null) played.UnionWith(data.played);
            if (data.flags != null) flags.UnionWith(data.flags);
        }

        // Nueva partida
        public void ResetAll()
        {
            SkipAll();
            played.Clear();
            flags.Clear();
            fallCount.Clear();
            Save();
        }

        void Save()
        {
            if (!usePlayerPrefs) return;
            PlayerPrefs.SetString(playerPrefsKey, JsonUtility.ToJson(GetSaveData()));
        }

        void LoadFromPlayerPrefs()
        {
            var json = PlayerPrefs.GetString(playerPrefsKey, "");
            if (!string.IsNullOrEmpty(json)) LoadSaveData(JsonUtility.FromJson<DialogueSaveData>(json));
        }
    }
}
