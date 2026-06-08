using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RPPG.TTS
{
    /// <summary>
    /// Dispatcher TTS multi-langues : choisit le bon ITTSProvider selon un code langue ISO 639-1.
    /// Le mapping est configuré dans l'inspector via une liste de paires (langCode, provider).
    /// Si la langue demandée n'a pas de mapping, on tombe sur le fallbackProviderBehaviour (optionnel).
    /// </summary>
    public class MultiLanguageTTSProvider : MonoBehaviour, ITTSProvider
    {
        [Serializable]
        public class LanguageMapping
        {
            [Tooltip("Code ISO 639-1 : \"en\", \"ja\", \"fr\", \"zh\"...")]
            public string languageCode;

            [Tooltip("Component MonoBehaviour qui implémente ITTSProvider")]
            public MonoBehaviour providerBehaviour;
        }

        [Header("Mappings langue → provider")]
        public LanguageMapping[] mappings = new LanguageMapping[]
        {
            new LanguageMapping { languageCode = "en", providerBehaviour = null },
            new LanguageMapping { languageCode = "ja", providerBehaviour = null },
        };

        [Header("Fallback (optionnel)")]
        [Tooltip("Provider utilisé si la langue demandée n'a pas de mapping. Si vide, retourne null.")]
        public MonoBehaviour fallbackProviderBehaviour;

        [Header("Debug")]
        public bool verboseLogs = true;

        Dictionary<string, ITTSProvider> _byLang;
        ITTSProvider _fallback;

        void Awake()
        {
            _byLang = new Dictionary<string, ITTSProvider>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in mappings)
            {
                if (m == null || string.IsNullOrEmpty(m.languageCode)) continue;
                if (m.providerBehaviour is ITTSProvider provider)
                {
                    _byLang[m.languageCode] = provider;
                }
                else if (m.providerBehaviour != null)
                {
                    Debug.LogWarning($"[MultiLangTTS] {m.providerBehaviour.GetType().Name} n'implémente pas ITTSProvider — mapping pour \"{m.languageCode}\" ignoré.");
                }
            }

            _fallback = fallbackProviderBehaviour as ITTSProvider;
        }

        public async Task<AudioClip> Synthesize(string text, string languageCode)
        {
            ITTSProvider chosen = null;
            string sourceLabel = "fallback";

            if (!string.IsNullOrEmpty(languageCode) && _byLang.TryGetValue(languageCode, out var direct))
            {
                chosen = direct;
                sourceLabel = languageCode;
            }
            else if (_fallback != null)
            {
                chosen = _fallback;
            }

            if (chosen == null)
            {
                Debug.LogError($"[MultiLangTTS] Aucun provider pour \"{languageCode}\" et pas de fallback configuré.");
                return null;
            }

            if (verboseLogs)
                Debug.Log($"[MultiLangTTS] \"{languageCode}\" → {((MonoBehaviour)chosen).GetType().Name} ({sourceLabel})");

            return await chosen.Synthesize(text, languageCode);
        }
    }
}
