using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using SherpaOnnx;
using UnityEngine;

namespace RPPG.TTS
{
    /// <summary>
    /// TTS provider basé sur sherpa-onnx + Kokoro 82M (modèle kokoro-multi-lang-v1_0), anglais.
    /// Deux seuls réglages réels chez Kokoro : la voix (voiceId) et la vitesse (speed).
    /// Pas de pitch/emotion/noise exposés — pour changer le timbre on change de voix.
    /// Voix EN femmes (sid) : 0 af_alloy, 1 af_aoede, 2 af_bella, 3 af_heart (défaut), 4 af_jessica,
    /// 5 af_kore, 6 af_nicole, 7 af_nova, 8 af_river, 9 af_sarah, 10 af_sky.
    /// Voix EN hommes (sid) : 11 am_adam, 12 am_echo, 13 am_eric, 14 am_fenrir, 15 am_liam,
    /// 16 am_michael, 17 am_onyx, 18 am_puck, 19 am_santa.
    /// </summary>
    public class KokoroOnyxTTSProvider : MonoBehaviour, ITTSProvider
    {
        [Header("Modèle (relatif à StreamingAssets/models/)")]
        [Tooltip("Dossier du modèle Kokoro dans StreamingAssets/models/")]
        public string modelFolderName = "kokoro-multi-lang-v1_0";

        [Tooltip("Nom du fichier .onnx")]
        public string modelFileName = "model.onnx";

        [Tooltip("Fichier des embeddings de voix (voices.bin)")]
        public string voicesFileName = "voices.bin";

        [Tooltip("Fichier tokens.txt")]
        public string tokensFileName = "tokens.txt";

        [Tooltip("Dossier espeak-ng-data (G2P)")]
        public string dataDirName = "espeak-ng-data";

        [Tooltip("Lexique anglais (vide = G2P espeak seul)")]
        public string lexiconFileName = "lexicon-us-en.txt";

        [Header("Voix")]
        [Range(0, 52)]
        [Tooltip("Speaker ID (0-52). 3 = af_heart (femme US, défaut). Femmes 0-10, hommes 11-19. Voir résumé en haut.")]
        public int voiceId = 3;

        [Header("Synthèse")]
        [Range(0.5f, 2f)]
        [Tooltip("Vitesse de parole. <1 = plus lent, >1 = plus rapide. Seul réglage de pacing pour Kokoro.")]
        public float speed = 1f;

        [Range(1, 16)]
        [Tooltip("Threads CPU utilisés par ONNX Runtime")]
        public int numThreads = 2;

        [Header("Filtre langue (pour dispatcher multi-TTS)")]
        [Tooltip("Si coché, ne synthétise que pour onlyLanguageCode")]
        public bool restrictToLanguage = false;

        [Tooltip("Code ISO 639-1 accepté (ex: \"en\"). Vide = accepte tout.")]
        public string onlyLanguageCode = "en";

        [Header("Debug")]
        public bool verboseLogs = true;

        OfflineTts _tts;
        int _sampleRate;
        volatile bool _initDone = false;
        volatile bool _initFailed = false;
        string _initError = "";

        void Start()
        {
            StartCoroutine(InitRoutine());
        }

        IEnumerator InitRoutine()
        {
            // Android : copie/extrait le modèle de StreamingAssets (jar) vers persistentDataPath.
            // Autres plateformes : no-op, StreamingAssets utilisé directement.
            string relDir = "models/" + modelFolderName;
            bool installed = false;
            yield return ModelInstaller.EnsureInstalled(relDir, modelFileName, ok => installed = ok);
            if (!installed)
            {
                _initError = $"Installation du modèle échouée ({relDir})";
                _initFailed = true;
                Debug.LogError($"[KokoroOnyx] {_initError}");
                yield break;
            }
            string root = ModelInstaller.ResolveRoot(relDir);
            Task.Run(() => InitEngine(root));
        }

        void InitEngine(string root)
        {
            try
            {
                string modelPath = Path.Combine(root, modelFileName);
                string voicesPath = Path.Combine(root, voicesFileName);
                string tokensPath = Path.Combine(root, tokensFileName);
                string dataDirPath = string.IsNullOrEmpty(dataDirName) ? "" : Path.Combine(root, dataDirName);
                string lexiconPath = string.IsNullOrEmpty(lexiconFileName) ? "" : Path.Combine(root, lexiconFileName);

                if (!File.Exists(modelPath))
                {
                    _initError = $"Modèle introuvable : {modelPath}";
                    _initFailed = true;
                    Debug.LogError($"[KokoroOnyx] {_initError}");
                    return;
                }
                if (!File.Exists(voicesPath))
                {
                    _initError = $"voices.bin introuvable : {voicesPath}";
                    _initFailed = true;
                    Debug.LogError($"[KokoroOnyx] {_initError}");
                    return;
                }
                if (!File.Exists(tokensPath))
                {
                    _initError = $"Tokens introuvable : {tokensPath}";
                    _initFailed = true;
                    Debug.LogError($"[KokoroOnyx] {_initError}");
                    return;
                }

                var config = new OfflineTtsConfig();
                config.Model.Kokoro.Model = modelPath;
                config.Model.Kokoro.Voices = voicesPath;
                config.Model.Kokoro.Tokens = tokensPath;
                config.Model.Kokoro.DataDir = dataDirPath;
                config.Model.Kokoro.Lexicon = (lexiconPath != "" && File.Exists(lexiconPath)) ? lexiconPath : "";
                config.Model.Kokoro.LengthScale = 1f;
                config.Model.NumThreads = numThreads;
                config.Model.Debug = 0;
                config.Model.Provider = "cpu";
                config.MaxNumSentences = 1;

                _tts = new OfflineTts(config);
                _sampleRate = _tts.SampleRate;
                _initDone = true;
                Debug.Log($"[KokoroOnyx] Init OK ({modelFolderName}, voiceId={voiceId}) — sampleRate={_sampleRate} Hz");
            }
            catch (Exception e)
            {
                _initError = e.Message;
                _initFailed = true;
                Debug.LogError($"[KokoroOnyx] Init exception : {e.Message}\n{e.StackTrace}");
            }
        }

        public async Task<AudioClip> Synthesize(string text, string languageCode)
        {
            if (restrictToLanguage && !string.IsNullOrEmpty(onlyLanguageCode) && languageCode != onlyLanguageCode)
            {
                if (verboseLogs)
                    Debug.Log($"[KokoroOnyx] Ignoré : provider pour \"{onlyLanguageCode}\", demande pour \"{languageCode}\"");
                return null;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[KokoroOnyx] Texte vide");
                return null;
            }

            float deadline = Time.realtimeSinceStartup + 30f;
            while (!_initDone && !_initFailed && Time.realtimeSinceStartup < deadline)
            {
                await Task.Yield();
            }
            if (_initFailed)
            {
                Debug.LogError($"[KokoroOnyx] Moteur non initialisé : {_initError}");
                return null;
            }
            if (!_initDone || _tts == null)
            {
                Debug.LogError("[KokoroOnyx] Init timeout (>30s)");
                return null;
            }

            if (verboseLogs)
                Debug.Log($"[KokoroOnyx] Synthèse \"{text}\" (lang={languageCode}, voiceId={voiceId}, speed={speed})");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            float[] samples = null;
            try
            {
                OfflineTtsGeneratedAudio audio = await Task.Run(() => _tts.Generate(text, speed, voiceId));
                if (audio != null)
                {
                    samples = audio.Samples;
                    audio.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[KokoroOnyx] Generate exception : {e.Message}");
                return null;
            }
            sw.Stop();

            if (samples == null || samples.Length == 0)
            {
                Debug.LogError("[KokoroOnyx] Génération vide");
                return null;
            }

            AudioClip clip = AudioClip.Create("KokoroOnyx", samples.Length, 1, _sampleRate, false);
            clip.SetData(samples, 0);

            if (verboseLogs)
                Debug.Log($"[KokoroOnyx] Généré {clip.length:F2}s en {sw.ElapsedMilliseconds} ms");

            return clip;
        }

        void OnDestroy()
        {
            try { _tts?.Dispose(); }
            catch { }
        }
    }
}
