# Lost Temple — Modules séparés

Cette branche contient le jeu **Lost Temple** découpé en **6 projets Unity autonomes**, un par capteur.
Chaque membre de l'équipe ouvre uniquement son dossier et teste son module seul.

| Dossier | Capteur | Salle(s) |
|---|---|---|
| `LostTemple_1_Accelerometre` | Accéléromètre | Pont suspendu (parcours en S + boule rouge NavMesh Agent à percuter), plateau à trous |
| `LostTemple_2_Gyroscope` | Gyroscope | Statues, rayon guidé par 3 miroirs, « Jacques a dit » (directions), rocher |
| `LostTemple_3_Camera` | Caméra | Esprit Coyote (photos de couleurs, mot de passe, clé), salle du Décor (scan de couleurs libre) |
| `LostTemple_4_Boussole` | Boussole (magnétomètre) | Aiguille magique, rose des vents, coffre aimanté, luciole poursuivie par une boule chasseuse |
| `LostTemple_5_Micro` | Microphone | Monstre à réveiller, boule d'énergie à la voix, phrase en anglais (reconnaissance vocale) |
| `LostTemple_6_Luminosite` | Luminosité (+ proximité) | Cachette et momies, luminosité du jeu inversée, grand brasier à éteindre avec la main |

Chaque projet contient le code commun (`Assets/LostTemple/Core`) et uniquement son module (`Assets/LostTemple/Modules/...`), avec un `README.md` qui explique comment le tester.

## Ouvrir un module

1. `git clone -b modules https://github.com/aminekas123/esprit_game_unity.git`
2. Unity Hub → **Add project from disk** → choisir le dossier de son module. Version : **Unity 6.6 (6000.6.5f1)**.
3. Menu **Lost Temple → Générer toutes les scènes**, puis ouvrir `Assets/LostTemple/Core/Scenes/MainMenu.unity` → **Play**.

Le jeu complet (les 8 salles ensemble) est sur la branche `main`.

Une scène modifiée à la main dans l'éditeur doit être listée dans `Modules/<module>/Scenes/HandEdited.txt` : le générateur ne l'écrasera pas (c'est le cas des salles 7 et 8 de la Caméra).
