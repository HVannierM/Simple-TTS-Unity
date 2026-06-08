using System;
using System.IO;
using System.Threading.Tasks;
using SherpaOnnx;
using UnityEngine;

namespace RPPG.TTS
{
    /// <summary>
    /// TTS provider basé sur sherpa-onnx (ONNX Runtime cross-platform : Win/Mac/Linux/Android/iOS).
    /// Conçu pour les modèles VITS-Piper (en_US-amy-medium par défaut), mais adaptable à d'autres
    /// modèles VITS en changeant les chemins inspector.
    /// Init du moteur au Start (background thread), Synthesize attend la fin de l'init si nécessaire.
    /// </summary>
    public class SherpaTTSProvider : MonoBehaviour, ITTSProvider
    {
        [Header("Modèle (relatif à StreamingAssets/models/)")]
        [Tooltip("Dossier contenant le modèle dans StreamingAssets/models/")]
        public string modelFolderName = "vits-piper-en_US-amy-medium";

        [Tooltip("Nom du fichier .onnx dans le dossier modèle")]
        public string modelFileName = "en_US-amy-medium.onnx";

        [Tooltip("Nom du fichier tokens.txt dans le dossier modèle")]
        public string tokensFileName = "tokens.txt";

        [Tooltip("Nom du dossier espeak-ng-data (vide si le modèle n'en utilise pas)")]
        public string dataDirName = "espeak-ng-data";

        [Header("Synthèse")]
        [Range(0.5f, 2f)]
        [Tooltip("Vitesse de lecture (1 = normal, <1 plus rapide)")]
        public float speed = 1f;

        [Range(0, 100)]
        [Tooltip("Speaker ID pour les modèles multi-speakers. 0 pour Piper Amy (single-speaker).")]
        public int defaultSpeakerId = 0;

        [Range(1, 16)]
        [Tooltip("Threads CPU utilisés par ONNX Runtime")]
        public int numThreads = 2;

        [Header("Filtre langue (pour dispatcher multi-TTS)")]
        [Tooltip("Si coché, ce provider n'accepte de synthétiser que pour le code langue spécifié (ISO 639-1)")]
        public bool restrictToLanguage = false;

        [Tooltip("Code ISO 639-1 de la langue accepté par ce provider (ex: \"en\", \"ja\"). Vide = accepte tout.")]
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
            Task.Run(InitEngine);
        }

        void InitEngine()
        {
            try
            {
                string root = Path.Combine(Application.streamingAssetsPath, "models", modelFolderName);

                string modelPath = Path.Combine(root, modelFileName);
                string tokensPath = Path.Combine(root, tokensFileName);
                string dataDirPath = string.IsNullOrEmpty(dataDirName) ? "" : Path.Combine(root, dataDirName);

                if (!File.Exists(modelPath))
                {
                    _initError = $"Modèle introuvable : {modelPath}";
                    _initFailed = true;
                    Debug.LogError($"[SherpaTTS] {_initError}");
                    return;
                }
                if (!File.Exists(tokensPath))
                {
                    _initError = $"Tokens introuvable : {tokensPath}";
                    _initFailed = true;
                    Debug.LogError($"[SherpaTTS] {_initError}");
                    return;
                }

                var config = new OfflineTtsConfig();
                config.Model.Vits.Model = modelPath;
                config.Model.Vits.Tokens = tokensPath;
                config.Model.Vits.DataDir = dataDirPath;
                config.Model.Vits.LengthScale = 1f;
                config.Model.Vits.NoiseScale = 0.667f;
                config.Model.Vits.NoiseScaleW = 0.8f;
                config.Model.NumThreads = numThreads;
                config.Model.Debug = 0;
                config.Model.Provider = "cpu";
                config.MaxNumSentences = 1;

                _tts = new OfflineTts(config);
                _sampleRate = _tts.SampleRate;
                _initDone = true;
                Debug.Log($"[SherpaTTS] Init OK ({modelFolderName}) — sampleRate={_sampleRate} Hz");
            }
            catch (Exception e)
            {
                _initError = e.Message;
                _initFailed = true;
                Debug.LogError($"[SherpaTTS] Init exception : {e.Message}\n{e.StackTrace}");
            }
        }

        public async Task<AudioClip> Synthesize(string text, string languageCode)
        {
            if (restrictToLanguage && !string.IsNullOrEmpty(onlyLanguageCode) && languageCode != onlyLanguageCode)
            {
                if (verboseLogs)
                    Debug.Log($"[SherpaTTS] Ignoré : provider pour \"{onlyLanguageCode}\", demande pour \"{languageCode}\"");
                return null;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[SherpaTTS] Texte vide");
                return null;
            }

            float deadline = Time.realtimeSinceStartup + 30f;
            while (!_initDone && !_initFailed && Time.realtimeSinceStartup < deadline)
            {
                await Task.Yield();
            }
            if (_initFailed)
            {
                Debug.LogError($"[SherpaTTS] Moteur non initialisé : {_initError}");
                return null;
            }
            if (!_initDone || _tts == null)
            {
                Debug.LogError("[SherpaTTS] Init timeout (>30s)");
                return null;
            }

            if (verboseLogs)
                Debug.Log($"[SherpaTTS] Synthèse \"{text}\" (lang={languageCode}, speakerId={defaultSpeakerId}, speed={speed})");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            float[] samples = null;
            try
            {
                OfflineTtsGeneratedAudio audio = await Task.Run(() => _tts.Generate(text, speed, defaultSpeakerId));
                if (audio != null)
                {
                    samples = audio.Samples;
                    audio.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SherpaTTS] Generate exception : {e.Message}");
                return null;
            }
            sw.Stop();

            if (samples == null || samples.Length == 0)
            {
                Debug.LogError("[SherpaTTS] Génération vide");
                return null;
            }

            AudioClip clip = AudioClip.Create("SherpaTTS", samples.Length, 1, _sampleRate, false);
            clip.SetData(samples, 0);

            if (verboseLogs)
                Debug.Log($"[SherpaTTS] Généré {clip.length:F2}s en {sw.ElapsedMilliseconds} ms");

            return clip;
        }

        void OnDestroy()
        {
            try { _tts?.Dispose(); }
            catch { }
        }
    }
}
