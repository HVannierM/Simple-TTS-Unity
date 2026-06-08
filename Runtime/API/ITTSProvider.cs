using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Interface publique des fournisseurs Text-to-Speech.
/// Le paramètre languageCode est un code ISO 639-1 ("en", "ja", "fr", "zh"...).
/// Chaque implémentation peut gérer une seule langue, ou dispatcher en interne.
/// </summary>
public interface ITTSProvider
{
    Task<AudioClip> Synthesize(string text, string languageCode);
}
