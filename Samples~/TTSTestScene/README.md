# TTS Test Scene — manual setup

This sample provides `TTSStandaloneTester.cs` to test the TTS pipeline through a simple UI.
**Build the scene yourself in ~5 minutes** by following the steps below.

## Prerequisites

1. Models downloaded — see [Documentation~/setup-models.md](../../Documentation~/setup-models.md) at the package root.
2. (For Japanese) A Unicode-capable font asset, e.g. **Noto Sans JP** as a TMP Font Asset. Without it, JP characters render as squares. See [Font setup](#-font-setup-japanese) at the bottom.

## Build the scene (5 min)

### 1. Create the scene

`File → New Scene → Empty 3D Scene` → save as `Assets/Scenes/TTSTestScene.unity`.

### 2. AudioSource

Create empty GameObject **`AudioSource`** → Add Component **Audio Source** → **uncheck Play On Awake**.

### 3. Provider GameObjects

Create one GameObject **per engine** you want, with the matching component:

- GameObject **`SherpaTTS`** → `SherpaTTSProvider`. Configure:
  - `Model Folder Name` = `vits-piper-en_US-amy-medium`
  - leave the rest at defaults

- GameObject **`VoicevoxTTS`** → `VoicevoxTTSProvider`. Configure:
  - `Specific Vvm Files` → Size 1, Element 0 = `24.vvm` (Akagi Mitama)
  - `Default Style Id` = `122` (Akagi Mitama Normal)
  - `Warmup On Init` = checked

### 4. Dispatcher

Create GameObject **`MultiLangTTS`** → `MultiLanguageTTSProvider`. Configure:
- `Mappings`:
  - Element 0: `Language Code` = `en`, `Provider Behaviour` = drag `SherpaTTS`
  - Element 1: `Language Code` = `ja`, `Provider Behaviour` = drag `VoicevoxTTS`

### 5. UI

`GameObject → UI → Canvas`. Inside the Canvas add:
- `UI → Input Field - TextMeshPro` → rename **`TextInput`**
- `UI → Dropdown - TextMeshPro` → rename **`LanguageDropdown`**
  - Set options: 0 = `English`, 1 = `Japanese`
- `UI → Button - TextMeshPro` → rename **`SpeakButton`**
- `UI → Text - TextMeshPro` → rename **`StatusLabel`**

### 6. Tester

Create GameObject **`Tester`** → add **`TTSStandaloneTester`** (this sample script). Inspector:
- `Tts Behaviour` → drag **`MultiLangTTS`**
- `Audio Source` → drag the AudioSource GameObject
- `Text Input` → drag `TextInput`
- `Language Dropdown` → drag `LanguageDropdown`
- `Speak Button` → drag `SpeakButton`
- `Status Label` → drag `StatusLabel`

### 7. Test

Press Play. The console should show `[Sherpa] Init OK` and `[Voicevox] Init OK`.
Type a phrase, switch the dropdown, click Speak.

## 🎨 Font setup (Japanese)

To render Japanese text correctly in the InputField:

1. Download **Noto Sans JP** (https://fonts.google.com/noto/specimen/Noto+Sans+JP)
2. Drag the `.ttf` into `Assets/Fonts/`
3. `Window → TextMeshPro → Font Asset Creator`
4. **Source Font File** = drag the Noto Sans JP TTF
5. **Atlas Population Mode** = `Dynamic` (rasterize glyphs on the fly — easiest)
6. Click `Generate Font Atlas`, then `Save` next to the TTF
7. (Optional) `Edit → Project Settings → TextMeshPro → Settings`:
   - `Default Font Asset` = the Noto Sans JP SDF
   - Add it to `Fallback Font Assets` list

Now any TMP_Text/TMP_InputField in the scene renders Japanese correctly.

## Going further

For minimal code usage from your own scripts:

```csharp
using RPPG.TTS;

public class MyScript : MonoBehaviour
{
    public MonoBehaviour ttsBehaviour;

    async void Start()
    {
        var tts = ttsBehaviour as ITTSProvider;
        AudioClip clip = await tts.Synthesize("Hello!", "en");
        GetComponent<AudioSource>().PlayOneShot(clip);
    }
}
```

Or with the static singleton (drop a `TTSManager` component anywhere):

```csharp
var clip = await TTSManager.Instance.Synthesize("やっほー", "ja");
```
