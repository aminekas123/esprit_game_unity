# Lost Temple — Modules séparés

Cette branche contient le jeu **Lost Temple** découpé en **6 projets Unity autonomes**, un par capteur.
Chaque membre de l'équipe ouvre uniquement son dossier et teste son module seul.

| Dossier | Capteur | Salle(s) |
|---|---|---|
| `LostTemple_1_Accelerometre` | Accéléromètre | Pont suspendu, plateau à trous |
| `LostTemple_2_Gyroscope` | Gyroscope | Statues, miroirs et rayon, rocher |
| `LostTemple_3_Camera` | Caméra | Esprit Coyote : photos, mot de passe, boss final |
| `LostTemple_4_Boussole` | Boussole (magnétomètre) | Aiguille magique, rose des vents, coffre aimanté, luciole |
| `LostTemple_5_Micro` | Microphone | Zone de silence, monstre, rythme, boule à la voix |
| `LostTemple_6_Luminosite` | Luminosité (+ proximité) | Cachette et momies, pont de cristal, code lumineux |

Chaque projet contient le code commun (`Assets/LostTemple/Core`) et uniquement son module (`Assets/LostTemple/Modules/...`), avec un `README.md` qui explique comment le tester.

## Ouvrir un module

1. `git clone -b modules https://github.com/aminekas123/esprit_game_unity.git`
2. Unity Hub → **Add project from disk** → choisir le dossier de son module. Version : **Unity 6.6 (6000.6.5f1)**.
3. Menu **Lost Temple → Générer toutes les scènes**, puis ouvrir `Assets/LostTemple/Core/Scenes/MainMenu.unity` → **Play**.

Le jeu complet (les 7 salles ensemble) est sur la branche `main`.
