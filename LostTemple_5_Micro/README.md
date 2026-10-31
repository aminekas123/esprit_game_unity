# Lost Temple — Module 5 : Microphone

Projet Unity **autonome** du module 5. Il contient le code commun (`Assets/LostTemple/Core`) et uniquement ce module (`Assets/LostTemple/Modules/5_Micro`). Il se teste seul, sans les autres modules.

## Contenu

**Salle 4 — Le monstre**

- Zone de silence à l'entrée : trop de bruit déclenche des fléchettes.
- Réveiller le monstre en parlant fort.
- **Nouveau :** reproduire un rythme en tapant dans ses mains pour l'hypnotiser.
- Pousser une boule d'énergie avec la voix jusqu'au monstre pour le détruire.

## Ouvrir et tester

1. Unity Hub → **Add project from disk** → choisir ce dossier. Version : **Unity 6.6 (6000.6.5f1)**.
2. Ouvrir `Assets/LostTemple/Core/Scenes/MainMenu.unity` et appuyer sur **Play**.
3. **Jouer** pour la partie normale, ou **Mode démo** pour arriver directement sur chaque énigme (pas de dégâts, bouton « Salle suivante »).
4. Le menu **Commandes / Capteurs** affiche si le capteur est détecté et sa valeur en direct.

| | |
|---|---|
| Geste sur téléphone | Le jeu mesure 1 s de bruit ambiant puis compare le niveau sonore à un seuil. Seul le volume est mesuré : rien n'est enregistré ni envoyé. |
| Dans l'éditeur | Micro du Mac, ou Espace = son fort |
| Commande de secours (capteur absent) | Bouton « Maintenir pour souffler » |

Déplacement de l'explorateur : joystick à l'écran, ou ZQSD / flèches dans l'éditeur.

## Fichiers du module

- `Assets/LostTemple/Modules/5_Micro/Scripts/MonsterWakePuzzle.cs`
- `Assets/LostTemple/Modules/5_Micro/Scripts/ClapRhythmPuzzle.cs`
- `Assets/LostTemple/Modules/5_Micro/Scripts/VoiceBallPuzzle.cs`
- `Assets/LostTemple/Modules/5_Micro/Scripts/QuietZone.cs`
- `Assets/LostTemple/Modules/5_Micro/Editor/Room4_Builder.cs`

## Regénérer les scènes

Menu **Lost Temple → Générer toutes les scènes** : recrée le menu et la ou les salles de ce module (écrase les scènes existantes).

## Android

Installer le module **Android Build Support** dans Unity Hub, puis File → Build Profiles → Android → Build.
