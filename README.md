# Simple TTS Unity

Plug-and-play neural Text-to-Speech for Unity. **Offline, cross-platform, no internet calls.**

Ships with two ready-to-use voices:
- 🇬🇧 **English** — Piper "Amy" (via [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx))
- 🇯🇵 **Japanese** — VOICEVOX "暁記ミタマ Akagi Mitama" (via [VoicevoxCoreSharp](https://github.com/yamachu/VoicevoxCoreSharp))

The architecture is pluggable via the `ITTSProvider` interface — easy to add more voices or languages.

✅ Windows · macOS · Linux · Android · iOS
✅ Single Unity-friendly async API: `Synthesize(text, langCode) → AudioClip`
✅ 100 % on-device, zero network requests
✅ Vendored: no external UPM dependencies, single package install

---

## Installation

In your Unity project's `Packages/manifest.json`, add:

```json
"com.rppg.tts": "https://github.com/HVannierM/Simple-TTS-Unity.git"
```

Or in Unity → **Window → Package Manager → `+` → Install from git URL** →
paste `https://github.com/HVannierM/Simple-TTS-Unity.git`.

To pin a specific version:

```
https://github.com/HVannierM/Simple-TTS-Unity.git#v0.1.0
```

## Required models (manual download)

Two neural voice models (~220 MB total) are NOT bundled — download them once:

➡️ **See [Documentation~/setup-models.md](Documentation~/setup-models.md) for step-by-step instructions.**

In short:
- Piper Amy → `Assets/StreamingAssets/models/vits-piper-en_US-amy-medium/`
- VOICEVOX dict + Akagi Mitama → `Assets/StreamingAssets/voicevox_core/`

## Quick start

### Option 1 — via inspector reference

```csharp
using RPPG.TTS;
using UnityEngine;

public class Demo : MonoBehaviour
{
    public MonoBehaviour ttsBehaviour;   // drag a provider GameObject here
    public AudioSource audioSource;

    async void Start()
    {
        var tts = ttsBehaviour as ITTSProvider;

        AudioClip en = await tts.Synthesize("Hey, how are you?", "en");
        audioSource.PlayOneShot(en);

        AudioClip jp = await tts.Synthesize("やっほー、元気だった？", "ja");
        audioSource.PlayOneShot(jp);
    }
}
```

### Option 2 — via global singleton

Drop a `TTSManager` component anywhere in your scene with a provider assigned.
Then from any script:

```csharp
AudioClip clip = await TTSManager.Instance.Synthesize("Hello", "en");
```

## Sample test scene

Install the sample via **Window → Package Manager → Simple TTS Unity →
Samples → Import "TTS Test Scene"**.

The sample includes `TTSStandaloneTester.cs` and a README explaining how to
build the test scene in ~5 minutes.

## Architecture

```
┌────────────────────────────────────────┐
│ Your code                              │
│   ITTSProvider.Synthesize(text, lang)  │
└────────────┬───────────────────────────┘
             │
       ┌─────▼─────────────────────┐
       │ MultiLanguageTTSProvider  │  (dispatch by ISO 639-1 code)
       └─────┬─────────────┬───────┘
             │             │
       "en"  │             │  "ja"
             ▼             ▼
   ┌──────────────┐  ┌──────────────────┐
   │ Sherpa-onnx  │  │ VOICEVOX Core    │
   │ (Piper Amy)  │  │ (Akagi Mitama)   │
   └──────────────┘  └──────────────────┘
```

### Add another language or voice

1. Implement `ITTSProvider` in a `MonoBehaviour`
2. Drop the component on a GameObject
3. Add a new `Mappings` entry on the `MultiLanguageTTSProvider` (e.g. `"fr"` → your provider)

## License

MIT — see [LICENSE.md](LICENSE.md).

⚠️ **VOICEVOX voices require credit attribution in your app credits.**
For Akagi Mitama, include the string **`VOICEVOX:暁記ミタマ`** somewhere in your credits screen.

Bundled third-party components have their own licenses — see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
