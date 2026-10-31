# Lost Temple — Module 6 : Luminosité (+ proximité)

Projet Unity **autonome** du module 6. Il contient le code commun (`Assets/LostTemple/Core`) et uniquement ce module (`Assets/LostTemple/Modules/6_Luminosite`). Il se teste seul, sans les autres modules.

## Contenu

**Salle 5 — La cachette et Salle 6 — Le pont invisible**

- Salle 5 (proximité) : couvrir le haut du téléphone 3 fois pour allumer les runes ; momies qui patrouillent, couvrir le capteur rend invisible 3 s.
- Salle 6 : faire apparaître un pont de cristal en changeant la lumière ; la lanterne s'use (secouer le téléphone).
- **Nouveau :** code lumineux à reproduire — ombres brèves ● et longues ▬ — pour ouvrir la porte.
- La lumière réelle de la pièce règle l'ambiance du temple.

## Ouvrir et tester

1. Unity Hub → **Add project from disk** → choisir ce dossier. Version : **Unity 6.6 (6000.6.5f1)**.
2. Ouvrir `Assets/LostTemple/Core/Scenes/MainMenu.unity` et appuyer sur **Play**.
3. **Jouer** pour la partie normale, ou **Mode démo** pour arriver directement sur chaque énigme (pas de dégâts, bouton « Salle suivante »).
4. Le menu **Commandes / Capteurs** affiche si le capteur est détecté et sa valeur en direct.

| | |
|---|---|
| Geste sur téléphone | Couvrez le capteur de lumière (près de la caméra avant) ou éclairez-le avec une lampe. |
| Dans l'éditeur | L = lumière, C = couvrir le capteur de proximité, R = secouer |
| Commande de secours (capteur absent) | Boutons « Lampe », « Couvrir », « Se cacher » |

Déplacement de l'explorateur : joystick à l'écran, ou ZQSD / flèches dans l'éditeur.

## Fichiers du module

- `Assets/LostTemple/Modules/6_Luminosite/Scripts/LightRevealPuzzle.cs`
- `Assets/LostTemple/Modules/6_Luminosite/Scripts/LightCodePuzzle.cs`
- `Assets/LostTemple/Modules/6_Luminosite/Scripts/ProximityPuzzle.cs`
- `Assets/LostTemple/Modules/6_Luminosite/Scripts/Enemy.cs`
- `Assets/LostTemple/Modules/6_Luminosite/Scripts/AmbientLightSync.cs`
- `Assets/LostTemple/Modules/6_Luminosite/Editor/Room5_Room6_Builder.cs`

## Regénérer les scènes

Menu **Lost Temple → Générer toutes les scènes** : recrée le menu et la ou les salles de ce module (écrase les scènes existantes).

## Android

Installer le module **Android Build Support** dans Unity Hub, puis File → Build Profiles → Android → Build.
