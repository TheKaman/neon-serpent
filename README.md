# Neon Serpent

A retro neon snake game for Android built in Unity 6.

## Features
- **3 Game Modes:** Classic Endless, Time Attack, Campaign
- **Power-ups:** Speed Boost, Shield, Score Multiplier, Ghost Mode, Shrink Pill, Poison
- **Leaderboards:** Google Play Games Services (global) with offline local fallback
- **Monetization:** Free with optional ads removal and cosmetic coin packs via IAP
- **Visual Style:** Retro neon on dark grid with URP Bloom post-processing

## Tech
- Unity 6 (URP 2D)
- Android API 24–35, IL2CPP, ARM64
- Google Play Games Services Unity Plugin v11+
- Unity LevelPlay (IronSource) SDK

## Development Setup
1. Install Unity 6 with Android Build Support module
2. Open project in Unity Hub from this folder
3. Install Android SDK/NDK via Unity Hub
4. Configure keystore at `E:\Keys\neonserpent.keystore` in Player Settings

## Git Workflow
- Work on `develop` branch
- Feature branches: `feature/<scope>-<name>`
- Merge to `main` only for Play Store releases
- See `CLAUDE.md` for commit conventions and architecture rules

## Build
Target: Google Play Store as signed `.aab` (Android App Bundle)
Package: `com.thekaman.neonserpent`
