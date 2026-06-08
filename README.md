# RPPG TTS

Plug-and-play neural Text-to-Speech for Unity. Offline, cross-platform.

Ships with two voices out of the box:
- 🇬🇧 **English** : Piper TTS, voice "Amy" (via [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx))
- 🇯🇵 **Japanese** : VOICEVOX, voice "暁記ミタマ Akagi Mitama" (via [VoicevoxCoreSharp](https://github.com/yamachu/VoicevoxCoreSharp))

Architecture pluggable via the `ITTSProvider` interface — add more voices or languages by writing your own provider.

- No internet calls, all on-device
- Cross-platform : Windows, macOS, Linux, Android, iOS
- Single Unity-friendly API : `ITTSProvider.Synthesize(text, langCode) → AudioClip`

## Quick start

```csharp
using RPPG.TTS;
using UnityEngine;

public class Demo : MonoBehaviour
{
    public MonoBehaviour ttsBehaviour;  // drag your TTS GameObject here
    public AudioSource audioSource;

    async void Start()
    {
        var tts = ttsBehaviour as ITTSProvider;
        AudioClip clip = await tts.Synthesize("Hey, how are you?", "en");
        audioSource.PlayOneShot(clip);

        AudioClip jp = await tts.Synthesize("やっほー、元気だった？", "ja");
        audioSource.PlayOneShot(jp);
    }
}
```

Or with the optional static singleton:

```csharp
AudioClip clip = await TTSManager.Instance.Synthesize("Hello", "en");
```

## Installation

Add to your project's `Packages/manifest.json` :

```json
"com.rppg.tts": "https://github.com/Hugo/rppg-tts.git"
```

Then in Unity → Window → Package Manager → select **RPPG TTS** → **Samples** → **Import** the test scene to validate.

## Required models

The neural models (~220 MB total) are auto-downloaded on first launch from a CDN.
See [Documentation~/setup-models.md](Documentation~/setup-models.md) for details, or to download them manually.

## License

MIT. See [LICENSE.md](LICENSE.md) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

VOICEVOX voices require credit attribution. For Akagi Mitama, the credit string is **`VOICEVOX:暁記ミタマ`** somewhere in your game credits.
