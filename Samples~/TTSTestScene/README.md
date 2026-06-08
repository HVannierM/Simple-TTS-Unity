# TTS Test Scene — ready-to-use sample

This sample provides a pre-configured Unity scene to test the TTS pipeline.

## Quick start

### 1. Prerequisites

- Voice models downloaded — see [Documentation~/setup-models.md](../../Documentation~/setup-models.md)
- (For Japanese) Noto Sans JP TMP font asset — see [Japanese font setup](#-japanese-font-setup) below

### 2. Open the scene

After importing this sample, the scene is at:
```
Assets/Samples/Simple TTS Unity/0.1.0/TTS Test Scene/TTSTestScene.unity
```

Double-click to open.

### 3. Reassign the font (if you set up Noto Sans JP)

The scene was saved using **Noto Sans JP SDF** as the TMP font. Since the font
file isn't bundled in the package, you'll see missing-font warnings on the
`TextInput`, `LanguageDropdown`, `SpeakButton` and `StatusLabel` TMP components.

Two options:
- **A.** Set up Noto Sans JP (see below), then in the inspector reassign your
  own `NotoSansJP-Regular SDF` to each TMP component.
- **B.** Reassign to `LiberationSans SDF` (default TMP) — English will work, but
  Japanese characters will render as squares.

### 4. Play

Press Play. Console should show:
```
[SherpaTTS] Init OK
[Voicevox] Init complète, moteur prêt
[TTSTester] Ready. Type some text and click Speak.
```

- Type a phrase in the input
- Switch the dropdown (English / Japanese)
- Click **Speak**

## 🎨 Japanese font setup

To render Japanese text correctly:

1. Download Noto Sans JP from https://fonts.google.com/noto/specimen/Noto+Sans+JP
2. Drop `NotoSansJP-Regular.ttf` into `Assets/Fonts/`
3. **Window → TextMeshPro → Font Asset Creator**
4. Source Font File = NotoSansJP-Regular
5. **Atlas Population Mode = Dynamic** (important)
6. Click **Generate Font Atlas**, then **Save**
7. (Optional) **Edit → Project Settings → TextMeshPro → Settings**:
   - Set `Default Font Asset` = your `NotoSansJP-Regular SDF`
   - Add it to `Fallback Font Assets`

## Going further — use TTS in your own code

```csharp
using RPPG.TTS;
using UnityEngine;

public class MyDemo : MonoBehaviour
{
    public MonoBehaviour ttsBehaviour;   // drag your provider in inspector
    public AudioSource audioSource;

    async void Start()
    {
        var tts = ttsBehaviour as ITTSProvider;

        AudioClip en = await tts.Synthesize("Hello!", "en");
        audioSource.PlayOneShot(en);

        AudioClip jp = await tts.Synthesize("やっほー", "ja");
        audioSource.PlayOneShot(jp);
    }
}
```

Or with the static singleton (drop a `TTSManager` component in the scene):

```csharp
var clip = await TTSManager.Instance.Synthesize("Bonjour", "en");
```

## Need to rebuild the scene from scratch?

If the included scene gets broken or you want to start over, the scene was
built with:

- `AudioSource` GameObject (Audio Source, PlayOnAwake unchecked)
- `SherpaTTS` GameObject (SherpaTTSProvider, `Model Folder Name = vits-piper-en_US-amy-medium`)
- `VoicevoxTTS` GameObject (VoicevoxTTSProvider, `Specific Vvm Files = ["24.vvm"]`, `Default Style Id = 122`)
- `MultiLangTTS` GameObject (MultiLanguageTTSProvider with `en → SherpaTTS`, `ja → VoicevoxTTS`)
- `Canvas` with TMP_InputField, TMP_Dropdown, Button, TMP_Text
- `Tester` GameObject (TTSStandaloneTester) with all references wired
