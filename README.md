# NOIZ - Rhythm Game

A rhythm game inspired by Beat Saber and Fruit Ninja, featuring beat-synced gameplay with lane-based notes and swipe mechanics.

## 🎮 Features

- **Beat-Synced Gameplay** - Notes spawn perfectly on beat using audio detection
- **Swipe or Tap Controls** - Cut notes with directional swipes or simple taps
- **Dynamic Difficulty** - Lane cooldown system prevents note spam
- **Deezer Integration** - Search and play tracks from Deezer API
- **Haptic Feedback** - Satisfying vibration feedback on cuts
- **Visual Effects** - Particle systems and smooth animations

## 🎵 How It Works

1. Search for an artist in the lobby
2. Select up to 5 tracks to play
3. Cut notes as they approach the cut zone
4. Match the swipe direction shown on each note
5. Build combos and achieve high scores

## 🛠️ Tech Stack

- Unity 6
- DOTween (animations)
- Nice Vibrations (haptics)
- Deezer API (music streaming)
- New Input System

## 🚀 Quick Start

1. Clone the repository
2. Open in Unity 6
3. Install required packages (DOTween, Input System)
4. Press Play - the game will automatically load tracks

## ⚙️ Settings

Configure via `NOIZSettings` ScriptableObject:
- Note speed and cooldown
- Timing windows (Perfect/Great/Bad)
- Volume ducking on cuts
- Haptic intensity

## 📱 Platforms

- Android
- iOS
- Windows (Editor testing)

## ⚠️ Disclaimer

This project uses the Deezer API for non-commercial, personal use only. All rights to music and artist data belong to their respective owners. This is a fan project not affiliated with Deezer.

## 🎨 Credits

- Music data: [Deezer API](https://developers.deezer.com)
- Haptics: [Nice Vibrations](https://assetstore.unity.com/packages/tools/audio/nice-vibrations-144937)
- Animations: [DOTween](http://dotween.demigiant.com)

---

**Made with passion for rhythm games** 🎵