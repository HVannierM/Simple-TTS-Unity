using System.Threading.Tasks;
using UnityEngine;

namespace RPPG.TTS
{
    /// <summary>
    /// Optional global singleton for easy access to TTS from anywhere.
    /// Wraps an ITTSProvider so any script can do TTSManager.Instance.Synthesize(text, lang).
    /// If no instance exists in the scene, returns null and logs a warning.
    /// To use: drop a TTSManager component on a GameObject and drag a provider into the inspector.
    /// </summary>
    public class TTSManager : MonoBehaviour, ITTSProvider
    {
        public static TTSManager Instance { get; private set; }

        [Tooltip("Provider used to synthesize. Usually a MultiLanguageTTSProvider that dispatches to per-language engines.")]
        public MonoBehaviour providerBehaviour;

        [Tooltip("If true, this GameObject survives scene loads.")]
        public bool dontDestroyOnLoad = true;

        ITTSProvider _provider;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[TTSManager] Multiple TTSManager instances found; destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            _provider = providerBehaviour as ITTSProvider;
            if (_provider == null && providerBehaviour != null)
                Debug.LogError($"[TTSManager] {providerBehaviour.GetType().Name} does not implement ITTSProvider.");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Task<AudioClip> Synthesize(string text, string languageCode)
        {
            if (_provider == null)
            {
                Debug.LogError("[TTSManager] No provider assigned in inspector.");
                return Task.FromResult<AudioClip>(null);
            }
            return _provider.Synthesize(text, languageCode);
        }
    }
}
