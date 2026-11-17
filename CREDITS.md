# Podziękowania i licencje

## Portrety postaci

Odświeżone portrety 19 bohaterów (10.10.2026) wymodelowano i wyrenderowano w Blenderze
skryptami `Tools/Blender/portraits.py`, `portrait_model.py`, `portrait_hair.py`,
`portrait_eyes.py` i `portraits_render.py`.
Geometria, materiały oraz studio są tworzone lokalnie, bez zewnętrznych modeli i tekstur.
Edytowalne sceny są w `Tools/Blender/Characters`, a gotowe PNG w `Assets/_Project/Art/Portraits`.

## Dźwięki kuchni

Wszystkie nagrania w `Assets/_Project/Audio/Kitchen` pochodzą z paczek na licencji **CC0 1.0** (domena publiczna,
https://creativecommons.org/publicdomain/zero/1.0/). Podpis nie jest wymagany. Dajemy go mimo to, z wdzięcznością.

| Paczka | Autor | Źródło |
|---|---|---|
| Impact Sounds | Kenney (www.kenney.nl) | https://kenney.nl/assets/impact-sounds |
| Interface Sounds | Kenney (www.kenney.nl) | https://kenney.nl/assets/interface-sounds |
| RPG Audio | Kenney (www.kenney.nl) | https://kenney.nl/assets/rpg-audio |
| 100 CC0 SFX | rubberduck | https://opengameart.org/content/100-cc0-sfx |
| 100 CC0 SFX #2 | rubberduck | https://opengameart.org/content/100-cc0-sfx-2 |
| 30 CC0 SFX loops | rubberduck | https://opengameart.org/content/30-cc0-sfx-loops |
| 40 CC0 water / splash / slime SFX | rubberduck | https://opengameart.org/content/40-cc0-water-splash-slime-sfx |

Każdy plik obrobiono w `RecordedSounds.Bake` (Zmywak → Audio → Bake Recorded Sounds):

- zmiksowano do mono i przepróbkowano do 44,1 kHz;
- przycięto ciszę;
- dodano krótkie wejście i wygaszenie;
- poziom wyrównano do syntetycznego odpowiednika (szczyt dla pojedynczych dźwięków, głośność dla pętli);
- pętle zszyto przenikaniem, żeby nie stukały na łączeniu.

| Plik | Nagranie | Paczka |
|---|---|---|
| Water_1.wav | sfx100v2_loop_water_01.ogg (4 s od 0,5 s) | 100 CC0 SFX #2 |
| ScrubSoft_1.wav | paper_02.ogg + paper_04.ogg + paper_03.ogg (sklejone w pętlę) | 100 CC0 SFX |
| ScrubHarsh_1.wav | saw.ogg | 30 CC0 SFX loops |
| Hum_1.wav | sfx100v2_loop_machine_01.ogg (4 s od 0,3 s) | 100 CC0 SFX #2 |
| Squeak_1.wav | creak3.ogg (1,25× szybciej) | RPG Audio |
| Squeak_2.wav | creak1.ogg (1,25× szybciej) | RPG Audio |
| Clink_1.wav | impactPlate_light_000.ogg | Impact Sounds |
| Clink_2.wav | impactPlate_light_001.ogg | Impact Sounds |
| Clink_3.wav | impactPlate_light_002.ogg | Impact Sounds |
| Clink_4.wav | impactPlate_light_003.ogg | Impact Sounds |
| Clink_5.wav | impactPlate_light_004.ogg | Impact Sounds |
| GlassClink_1.wav | impactGlass_light_000.ogg | Impact Sounds |
| GlassClink_2.wav | impactGlass_light_001.ogg | Impact Sounds |
| GlassClink_3.wav | impactGlass_light_002.ogg | Impact Sounds |
| GlassClink_4.wav | impactGlass_light_003.ogg | Impact Sounds |
| GlassClink_5.wav | impactGlass_light_004.ogg | Impact Sounds |
| Crash_1.wav | glass_01.ogg | 100 CC0 SFX |
| Crash_2.wav | glass_02.ogg | 100 CC0 SFX |
| Crash_3.wav | sfx100v2_glass_02.ogg | 100 CC0 SFX #2 |
| Crash_4.wav | sfx100v2_glass_03.ogg | 100 CC0 SFX #2 |
| Scrape_1.wav | tools_01.ogg | 100 CC0 SFX |
| Scrape_2.wav | tools_04.ogg | 100 CC0 SFX |
| Pickup_1.wav | impactGeneric_light_000.ogg | Impact Sounds |
| Pickup_2.wav | impactGeneric_light_001.ogg | Impact Sounds |
| Pickup_3.wav | impactGeneric_light_002.ogg | Impact Sounds |
| Ding_1.wav | bell_02.ogg | 100 CC0 SFX |
| Fault_1.wav | error_006.ogg | Interface Sounds |
| PowerUp_1.wav | maximize_008.ogg | Interface Sounds |
| Clang_1.wav | metal_05.ogg | 100 CC0 SFX |
| Clang_2.wav | metal_08.ogg | 100 CC0 SFX |
| Clang_3.wav | metal_09.ogg | 100 CC0 SFX |
| Chop_1.wav | chop.ogg | RPG Audio |
| Chop_2.wav | impactWood_light_000.ogg | Impact Sounds |
| Chop_3.wav | impactWood_light_002.ogg | Impact Sounds |
| Splash_1.wav | splash_09.ogg | 40 CC0 water / splash / slime SFX |
| Splash_2.wav | splash_10.ogg | 40 CC0 water / splash / slime SFX |
| Click_1.wav | switch_01.ogg | 100 CC0 SFX |

Radio i muzyka są nadal syntetyzowane w kodzie (`RadioMusic`), bez cudzych nagrań.

## Dźwięki interfejsu, wyniku i pokoju

Pliki w `Assets/_Project/Audio/Game` pochodzą z tych samych paczek CC0 co dźwięki kuchni albo są syntetyzowane w kodzie
(`GameSynth`: dżingle wyniku, muzyka menu, cichy szum pokoju). Obróbka w `GameSoundsBake.Bake` (Zmywak → Audio → Bake Game
Sounds): mono, 44,1 kHz, przycięcie, wejście i wygaszenie, wyrównanie poziomu. Dźwięki „zza ściany” (drzwi, kroki, ulica,
grzmot, skrzypienie) przepuszczono przez filtr dolnoprzepustowy, jakby dochodziły przez mur, okno albo strop.

| Plik | Nagranie | Paczka |
|---|---|---|
| UiClick_1.wav | click_002.ogg | Interface Sounds |
| UiClick_2.wav | click_003.ogg | Interface Sounds |
| UiConfirm_1.wav | confirmation_002.ogg | Interface Sounds |
| UiBack_1.wav | back_002.ogg | Interface Sounds |
| UiError_1.wav | error_003.ogg | Interface Sounds |
| UiToggle_1.wav | toggle_002.ogg | Interface Sounds |
| UiTick_1.wav | tick_002.ogg | Interface Sounds |
| UiOpen_1.wav | maximize_003.ogg | Interface Sounds |
| PageTurn_1.wav | bookFlip2.ogg | RPG Audio |
| Reward_1.wav | handleCoins.ogg | RPG Audio |
| Reward_2.wav | handleCoins2.ogg | RPG Audio |
| ShiftEnd_1.wav | bell_01.ogg | 100 CC0 SFX |
| AmbRain_1.wav | rain.ogg (filtr 6 kHz, pętla) | 30 CC0 SFX loops |
| AmbCity_1.wav | sfx100v2_loop_highway.ogg (filtr 600 Hz, pętla) | 100 CC0 SFX #2 |
| Creak_1.wav | creak2.ogg (0,8× wolniej, filtr 2,5 kHz) | RPG Audio |
| Door_1.wav | doorClose_1.ogg (filtr 900 Hz) | RPG Audio |
| Door_2.wav | doorClose_3.ogg (filtr 900 Hz) | RPG Audio |
| Footsteps_1.wav | footstep_wood_000/001/002.ogg (filtr 500 Hz) | Impact Sounds |
| Thunder_1.wav | sfx100v2_thunder_01.ogg (filtr 1,5 kHz) | 100 CC0 SFX #2 |
| DialogueBlip_1.wav, MoneyTick_1.wav, GradeGood_1.wav, GradePoor_1.wav, Promotion_1.wav, FlowChime_1.wav, FullRack_1.wav, WrongTool_1.wav, MenuMusic_1.wav, AmbRoom_1.wav | syntetyzowane (`GameSynth`) | - |
