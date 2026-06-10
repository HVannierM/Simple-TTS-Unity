using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using VoicevoxCoreSharp.Core;
using VoicevoxCoreSharp.Core.Enum;
using VoicevoxCoreSharp.Core.Struct;

namespace RPPG.TTS
{
    /// <summary>
    /// TTS provider basé sur VOICEVOX Core via le binding VoicevoxCoreSharp (yamachu).
    /// Cross-platform : Win/Mac/Linux/Android/iOS.
    /// Pipeline : OpenJTalk (analyse JP) → ONNX Runtime → VoiceModel (.vvm) → WAV bytes → AudioClip.
    /// Le styleId identifie une voix précise (Akagi Mitama Normal = 122).
    /// Au premier lancement, logMetasOnInit dump les Metas pour identifier les styleIds.
    /// </summary>
    public class VoicevoxTTSProvider : MonoBehaviour, ITTSProvider
    {
        [Header("Chemins (relatifs à StreamingAssets/voicevox_core/)")]
        [Tooltip("Dossier du dict OpenJTalk dans StreamingAssets/voicevox_core/dict/")]
        public string openJtalkDictName = "open_jtalk_dic_utf_8-1.11";

        [Tooltip("Dossier des modèles .vvm dans StreamingAssets/voicevox_core/models/vvms/")]
        public string vvmsFolder = "models/vvms";

        [Tooltip("Si vide, charge TOUS les .vvm du dossier. Sinon, charge uniquement les fichiers listés.")]
        public string[] specificVvmFiles = new string[0];

        [Header("Voix")]
        [Tooltip("Style ID de la voix à utiliser. Akagi Mitama Normal = 122.")]
        public uint defaultStyleId = 0;

        [Header("Filtre langue (pour dispatcher multi-TTS)")]
        public bool restrictToLanguage = false;

        [Tooltip("Code ISO 639-1 de la langue accepté par ce provider (ex: \"ja\", \"en\"). Vide = accepte tout.")]
        public string onlyLanguageCode = "ja";

        [Header("Performance")]
        [Tooltip("Au démarrage, lance une synthèse à vide pour 'chauffer' les modèles ONNX. " +
                 "Sans warmup, la 1ère vraie synthèse prend ~50s.")]
        public bool warmupOnInit = true;

        [Tooltip("Phrase courte utilisée pour le warmup")]
        public string warmupText = "あ";

        [Header("Debug")]
        [Tooltip("Au démarrage, log la liste de tous les speakers/styles chargés")]
        public bool logMetasOnInit = true;

        public bool verboseLogs = true;

        Synthesizer _synthesizer;
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
                string root = Path.Combine(Application.streamingAssetsPath, "voicevox_core");
                string dictPath = Path.Combine(root, "dict", openJtalkDictName);
                string vvmsPath = Path.Combine(root, vvmsFolder);

                if (!Directory.Exists(dictPath))
                {
                    _initError = $"OpenJTalk dict introuvable : {dictPath}";
                    _initFailed = true;
                    Debug.LogError($"[Voicevox] {_initError}");
                    return;
                }
                if (!Directory.Exists(vvmsPath))
                {
                    _initError = $"Dossier vvms introuvable : {vvmsPath}";
                    _initFailed = true;
                    Debug.LogError($"[Voicevox] {_initError}");
                    return;
                }

                Debug.Log("[Voicevox] Init en cours…");

                var initOptions = InitializeOptions.Default();
                // Workaround binding VoicevoxCoreSharp 0.16.0 : la lib Rust attend une C-string
                // null-terminée, mais le binding ne l'ajoute pas → lecture de garbage après la string.
                string dictPathNullTerminated = dictPath + "\0";
                if (OpenJtalk.New(dictPathNullTerminated, out var openJtalk) != ResultCode.RESULT_OK)
                {
                    _initError = "OpenJtalk.New failed";
                    _initFailed = true;
                    Debug.LogError($"[Voicevox] {_initError}");
                    return;
                }

                string ortLibPath = ResolveOnnxRuntimePath();
                if (string.IsNullOrEmpty(ortLibPath))
                {
                    _initError = "Lib voicevox_onnxruntime introuvable pour la plateforme courante";
                    _initFailed = true;
                    Debug.LogError($"[Voicevox] {_initError}");
                    return;
                }

                // Workaround binding VoicevoxCoreSharp 0.16.0 : null-terminate le path
                // (la lib Rust attend une C-string, le binding ne l'ajoute pas).
                var loadOrtOptions = new LoadOnnxruntimeOptions(ortLibPath + "\0");
                if (Onnxruntime.LoadOnce(loadOrtOptions, out var onnxruntime) != ResultCode.RESULT_OK)
                {
                    _initError = $"Onnxruntime.LoadOnce failed (lib={ortLibPath})";
                    _initFailed = true;
                    Debug.LogError($"[Voicevox] {_initError}");
                    return;
                }

                if (Synthesizer.New(onnxruntime, openJtalk, initOptions, out var synth) != ResultCode.RESULT_OK)
                {
                    _initError = "Synthesizer.New failed";
                    _initFailed = true;
                    Debug.LogError($"[Voicevox] {_initError}");
                    return;
                }
                using (openJtalk) { }

                string[] vvmFiles = specificVvmFiles.Length > 0
                    ? specificVvmFiles
                    : Directory.GetFiles(vvmsPath, "*.vvm");

                int loaded = 0;
                foreach (var vvmFileRelOrAbs in vvmFiles)
                {
                    string fullPath = Path.IsPathRooted(vvmFileRelOrAbs)
                        ? vvmFileRelOrAbs
                        : Path.Combine(vvmsPath, Path.GetFileName(vvmFileRelOrAbs));

                    // Workaround binding VoicevoxCoreSharp 0.16.0 : null-terminate la path
                    // (la lib Rust attend une C-string, le binding ne l'ajoute pas).
                    if (VoiceModelFile.New(fullPath + "\0", out var voiceModel) != ResultCode.RESULT_OK)
                    {
                        Debug.LogWarning($"[Voicevox] Impossible de charger {fullPath}");
                        continue;
                    }
                    if (synth.LoadVoiceModel(voiceModel) != ResultCode.RESULT_OK)
                    {
                        Debug.LogWarning($"[Voicevox] LoadVoiceModel failed pour {fullPath}");
                        voiceModel.Dispose();
                        continue;
                    }
                    voiceModel.Dispose();
                    loaded++;
                }

                _synthesizer = synth;
                Debug.Log($"[Voicevox] Modèles chargés : {loaded}/{vvmFiles.Length}");

                if (logMetasOnInit)
                {
                    LogMetas();
                }

                if (warmupOnInit && !string.IsNullOrEmpty(warmupText))
                {
                    var swWarmup = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        string warmupNullTerm = warmupText + "\0";
                        var rc = _synthesizer.Tts(warmupNullTerm, defaultStyleId, TtsOptions.Default(),
                            out var _, out var _);
                        swWarmup.Stop();
                        Debug.Log($"[Voicevox] Warmup ({warmupText}, styleId={defaultStyleId}) : {swWarmup.ElapsedMilliseconds} ms → rc={rc}");

                        if (rc == ResultCode.RESULT_STYLE_NOT_FOUND_ERROR)
                        {
                            Debug.LogError($"[Voicevox] ⚠️ Le styleId={defaultStyleId} n'existe PAS dans les .vvm chargés. " +
                                $"Vérifie 'Default Style Id' dans l'inspector. Pour Akagi Mitama Normal = 122 (24.vvm).");
                        }
                    }
                    catch (Exception e)
                    {
                        swWarmup.Stop();
                        Debug.LogWarning($"[Voicevox] Warmup échoué ({swWarmup.ElapsedMilliseconds} ms) : {e.Message}");
                    }
                }

                _initDone = true;
                Debug.Log($"[Voicevox] Init complète, moteur prêt");
            }
            catch (Exception e)
            {
                _initError = e.Message;
                _initFailed = true;
                Debug.LogError($"[Voicevox] Init exception : {e.Message}\n{e.StackTrace}");
            }
        }

        void LogMetas()
        {
            try
            {
                var metasJson = _synthesizer.MetasJson;
                Debug.Log($"[Voicevox] === SPEAKERS METAS ===\n{metasJson}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Voicevox] LogMetas a échoué : {e.Message}");
            }
        }

        string ResolveOnnxRuntimePath()
        {
#if UNITY_EDITOR_WIN
            return CombineEditorPluginPath("win-x64", "voicevox_onnxruntime.dll");
#elif UNITY_STANDALONE_WIN
            return Path.Combine(Application.dataPath, "Plugins", "x86_64", "voicevox_onnxruntime.dll");
#elif UNITY_EDITOR_OSX
            return CombineEditorPluginPath("osx-arm64", "libvoicevox_onnxruntime.dylib");
#elif UNITY_STANDALONE_OSX
            return Path.Combine(Application.dataPath, "Plugins", "libvoicevox_onnxruntime.dylib");
#elif UNITY_ANDROID
            return "libvoicevox_onnxruntime.so";
#elif UNITY_IOS
            return "voicevox_onnxruntime.framework";
#else
            return null;
#endif
        }

        static string CombineEditorPluginPath(string runtime, string fileName)
        {
            string packageRoot = ResolvePackageRoot();
            if (string.IsNullOrEmpty(packageRoot))
            {
                Debug.LogError("[Voicevox] Could not locate the com.rppg.tts package root on disk.");
                return null;
            }
            return Path.Combine(packageRoot, "Runtime", "Plugins", "Voicevox", "runtimes", runtime, "native", fileName);
        }

        /// <summary>
        /// Resolves the absolute path of the com.rppg.tts package on disk,
        /// whether installed via Git URL / local tarball
        /// (Library/PackageCache/com.rppg.tts@&lt;hash&gt;) or embedded
        /// (Packages/com.rppg.tts).
        /// </summary>
        /// <remarks>
        /// PackageCache is checked first because, when the package is installed
        /// via Git URL, Unity exposes "Packages/com.rppg.tts" as a virtual mount
        /// that <see cref="System.IO.Directory.Exists"/> resolves to <c>true</c>
        /// but Windows' native <c>LoadLibrary</c> cannot open. Using the real
        /// PackageCache path lets the OS load native DLLs correctly.
        /// </remarks>
        static string ResolvePackageRoot()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return null;

            string cacheDir = Path.Combine(projectRoot, "Library", "PackageCache");
            if (Directory.Exists(cacheDir))
            {
                var matches = Directory.GetDirectories(cacheDir, "com.rppg.tts@*");
                if (matches != null && matches.Length > 0) return matches[0];

                var fallback = Directory.GetDirectories(cacheDir, "com.rppg.tts*");
                if (fallback != null && fallback.Length > 0) return fallback[0];
            }

            string embedded = Path.Combine(projectRoot, "Packages", "com.rppg.tts");
            if (Directory.Exists(embedded)) return embedded;

            return null;
        }

        public async Task<AudioClip> Synthesize(string text, string languageCode)
        {
            if (restrictToLanguage && !string.IsNullOrEmpty(onlyLanguageCode) && languageCode != onlyLanguageCode)
            {
                if (verboseLogs)
                    Debug.Log($"[Voicevox] Ignoré : provider pour \"{onlyLanguageCode}\", demande pour \"{languageCode}\"");
                return null;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[Voicevox] Texte vide");
                return null;
            }

            float deadline = Time.realtimeSinceStartup + 60f;
            while (!_initDone && !_initFailed && Time.realtimeSinceStartup < deadline)
            {
                await Task.Yield();
            }
            if (_initFailed)
            {
                Debug.LogError($"[Voicevox] Moteur KO : {_initError}");
                return null;
            }
            if (!_initDone || _synthesizer == null)
            {
                Debug.LogError("[Voicevox] Init timeout (>60s)");
                return null;
            }

            // Workaround binding VoicevoxCoreSharp 0.16.0 : la lib Rust attend une C-string
            // null-terminée, mais le binding ne l'ajoute pas → RESULT_INVALID_UTF8_INPUT_ERROR aléatoire.
            string normalizedText;
            try
            {
                byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(text);
                normalizedText = System.Text.Encoding.UTF8.GetString(utf8Bytes) + "\0";
            }
            catch (Exception e)
            {
                Debug.LogError($"[Voicevox] UTF-8 normalisation échouée : {e.Message}");
                return null;
            }

            if (verboseLogs)
                Debug.Log($"[Voicevox] Synthèse \"{text}\" (styleId={defaultStyleId})");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            byte[] wavBytes = null;
            try
            {
                await Task.Run(() =>
                {
                    var rc = _synthesizer.Tts(normalizedText, defaultStyleId, TtsOptions.Default(),
                        out var outputWavSize, out var outputWav);
                    if (rc == ResultCode.RESULT_OK)
                    {
                        wavBytes = outputWav;
                    }
                    else
                    {
                        Debug.LogError($"[Voicevox] Tts() returned {rc} (text=\"{normalizedText}\", styleId={defaultStyleId})");
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[Voicevox] Tts exception : {e.Message}");
                return null;
            }
            sw.Stop();

            if (wavBytes == null || wavBytes.Length == 0)
            {
                Debug.LogError("[Voicevox] Audio vide");
                return null;
            }

            AudioClip clip = WavUtility.ToAudioClip(wavBytes, "VoicevoxTTS");
            if (verboseLogs)
                Debug.Log($"[Voicevox] Généré {clip?.length:F2}s en {sw.ElapsedMilliseconds} ms");

            return clip;
        }

        void OnDestroy()
        {
            try { _synthesizer?.Dispose(); }
            catch { }
        }
    }
}
