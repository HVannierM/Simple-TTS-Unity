using System.Diagnostics;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RPPG.TTS;
using Debug = UnityEngine.Debug;

namespace RPPG.TTS.Samples
{
    /// <summary>
    /// Standalone tester for any ITTSProvider. Wires a TextMeshPro UI (input + dropdown + button + status)
    /// to a TTS provider and plays the synthesized audio in an AudioSource.
    /// Drop on a GameObject and assign references in the inspector. See README.md for setup steps.
    /// </summary>
    public class TTSStandaloneTester : MonoBehaviour
    {
        [Header("TTS")]
        [Tooltip("Component implementing ITTSProvider")]
        public MonoBehaviour ttsBehaviour;

        public AudioSource audioSource;

        [Header("UI")]
        public TMP_InputField textInput;
        public TMP_Dropdown languageDropdown;
        public Button speakButton;
        public TMP_Text statusLabel;

        [Header("Fallback (if no UI)")]
        [TextArea(2, 4)]
        public string fallbackText = "Hello, my name is Amy. Nice to meet you!";

        [Tooltip("ISO 639-1 code used if no dropdown: \"en\", \"ja\", etc.")]
        public string fallbackLanguageCode = "en";

        ITTSProvider _tts;
        Stopwatch _sw;

        public string CurrentText
        {
            get => textInput != null ? textInput.text : fallbackText;
            set
            {
                if (textInput != null) textInput.text = value;
                else fallbackText = value;
            }
        }

        // Dropdown index convention: 0 = English, 1 = Japanese, 2 = French.
        // If you add more options in the dropdown, extend this map accordingly.
        public string CurrentLanguageCode
        {
            get
            {
                if (languageDropdown == null) return fallbackLanguageCode;
                switch (languageDropdown.value)
                {
                    case 1: return "ja";
                    case 2: return "fr";
                    default: return "en";
                }
            }
            set
            {
                if (languageDropdown == null) { fallbackLanguageCode = value; return; }
                if (value == "ja" || value == "jp") languageDropdown.value = 1;
                else if (value == "fr") languageDropdown.value = 2;
                else languageDropdown.value = 0;
            }
        }

        public event System.Action<AudioClip> OnSpeakComplete;
        public event System.Action OnSpeakFailed;

        public void Speak() => OnSpeakClicked();

        public async void Speak(string text, string languageCode)
        {
            CurrentText = text;
            CurrentLanguageCode = languageCode;
            await SpeakInternal(text, languageCode);
        }

        void Start()
        {
            _tts = ttsBehaviour as ITTSProvider;
            if (_tts == null)
            {
                Debug.LogError("[TTSTester] ttsBehaviour does not implement ITTSProvider");
                UpdateStatus("ERROR: TTS provider not assigned");
                return;
            }
            if (audioSource == null)
            {
                Debug.LogError("[TTSTester] audioSource missing");
                UpdateStatus("ERROR: AudioSource not assigned");
                return;
            }

            if (speakButton != null) speakButton.onClick.AddListener(OnSpeakClicked);
            UpdateStatus("Ready. Type some text and click Speak.");
        }

        public async void OnSpeakClicked()
        {
            string text = !string.IsNullOrWhiteSpace(CurrentText) ? CurrentText : fallbackText;
            string langCode = CurrentLanguageCode;
            await SpeakInternal(text, langCode);
        }

        async Task SpeakInternal(string text, string langCode)
        {
            if (_tts == null)
            {
                Debug.LogError("[TTSTester] No TTS provider");
                OnSpeakFailed?.Invoke();
                return;
            }

            Debug.Log($"[TTSTester] Synthesizing \"{text}\" in \"{langCode}\"");
            UpdateStatus($"Synthesizing… ({langCode})");

            _sw = Stopwatch.StartNew();
            AudioClip clip = await _tts.Synthesize(text, langCode);
            _sw.Stop();

            if (clip == null)
            {
                UpdateStatus($"ERROR: no clip generated (see console). {_sw.ElapsedMilliseconds} ms");
                OnSpeakFailed?.Invoke();
                return;
            }

            if (audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            UpdateStatus($"Played: {clip.length:F2}s, generated in {_sw.ElapsedMilliseconds} ms");
            OnSpeakComplete?.Invoke(clip);
        }

        void UpdateStatus(string msg)
        {
            Debug.Log($"[TTSTester] {msg}");
            if (statusLabel != null) statusLabel.text = msg;
        }
    }
}
