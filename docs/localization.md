# Localization

The mod uses RimWorld's built-in `Verse.Translator` system for all user-facing strings. Translations are managed in a separate repository and included as a git submodule.

## Languages Submodule

Translation files live in the [Multiplayer-Locale](https://github.com/rwmt/Multiplayer-Locale) repository, included as a git submodule at `Languages/`:

```
Languages/
├── ChineseSimplified/Keyed/
├── ChineseTraditional/Keyed/
├── Czech/Keyed/
├── Danish/Keyed/
├── Dutch/Keyed/
├── English/Keyed/
├── Finnish/Keyed/
├── French/Keyed/
├── German/Keyed/
├── Italian/Keyed/
├── Korean/Keyed/
├── Portuguese/Keyed/
├── PortugueseBrazilian/Keyed/
├── Russian/Keyed/
├── Spanish/Keyed/
├── SpanishLatin/Keyed/
├── Turkish/Keyed/
└── Ukrainian/Keyed/
```

Initialize or update the submodule:

```bash
git submodule update --init
```

## Key Convention

All mod-specific translation keys use the `Mp` prefix with PascalCase:

| Pattern | Example |
|---------|---------|
| UI elements | `MpChatButton`, `MpHostServer`, `MpConnectButton` |
| Settings | `MpAutoAcceptSteam`, `MpAutosaveSlots`, `MpUsernameSetting` |
| Descriptions | `MpAsyncTimeDesc`, `MpAutoAcceptSteamDesc` |
| Sessions | `MpCaravanFormingSession`, `MpCaravanSplittingSession` |
| Errors/alerts | `MpAlertPing`, `MpAlertPingDesc1` |

The codebase currently uses ~223 translation keys across 48 files.

## Usage in Code

Use RimWorld's `.Translate()` extension method on string literals:

```csharp
// Simple key lookup
string label = "MpChatButton".Translate();

// With parameters
string msg = "MpConnectedAs".Translate(username);
```

### Multi-Section Translations

For multi-part text (e.g., error messages with numbered paragraphs), use the helper:

```csharp
// Joins MpLoadingError1, MpLoadingError2, etc. with double newlines
string text = MpUtil.TranslateWithDoubleNewLines("MpLoadingError", count: 3);
```

This is defined in `Source/Client/Util/MpUtil.cs`.

## Translation Mod Detection

The mod identifies translation-only mods to allow filtering them in the mod compatibility UI:

```csharp
// Source/Client/MultiplayerData.cs
MultiplayerData.IsTranslationMod(ModMetaData mod)
```

A mod is classified as translation-only if it has no C# assemblies, no `Defs/`, no `Patches/`, but has a `Languages/` directory. The `hideTranslationMods` setting controls whether these are shown in the mod list.

## Contributing Translations

To add or update translations:

1. Fork the [Multiplayer-Locale](https://github.com/rwmt/Multiplayer-Locale) repository.
2. Edit or add XML files in `Languages/{LanguageCode}/Keyed/`.
3. Use the English files as a reference for key names and parameter placeholders.
4. Open a PR against `Multiplayer-Locale`.

Translation XML follows RimWorld's standard format:

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
    <MpChatButton>Chat</MpChatButton>
    <MpConnectedAs>Connected as {0}</MpConnectedAs>
</LanguageData>
```

## Adding New Translation Keys

When adding new user-facing strings to the mod:

1. Add the `.Translate()` call in the C# code with an `Mp`-prefixed key.
2. Add the English default in `Languages/English/Keyed/` in the Multiplayer-Locale repo.
3. Update the submodule reference in this repo once the Locale PR is merged:

```bash
cd Languages
git pull origin main
cd ..
git add Languages
git commit -m "Update Languages submodule"
```
