# Lost Temple — Module 1 : Accéléromètre

Projet Unity **autonome** du module 1. Il contient le code commun (`Assets/LostTemple/Core`) et uniquement ce module (`Assets/LostTemple/Modules/1_Accelerometre`). Il se teste seul, sans les autres modules.

## Contenu

**Salle 1 — Le pont suspendu**

- Guider la boule d'or sur un pont en zigzag au-dessus du vide (barre tournante en piège).
- **Nouveau :** plateau à trous — mener une 2e boule au coin doré sans passer sur les trous noirs.

## Ouvrir et tester

1. Unity Hub → **Add project from disk** → choisir ce dossier. Version : **Unity 6.6 (6000.6.5f1)**.
2. Ouvrir `Assets/LostTemple/Core/Scenes/MainMenu.unity` et appuyer sur **Play**.
3. **Jouer** pour la partie normale, ou **Mode démo** pour arriver directement sur chaque énigme (pas de dégâts, bouton « Salle suivante »).
4. Le menu **Commandes / Capteurs** affiche si le capteur est détecté et sa valeur en direct.

| | |
|---|---|
| Geste sur téléphone | Inclinez le téléphone (tenu comme pour lire). Il est calibré au début de chaque épreuve. |
| Dans l'éditeur | Flèches / ZQSD = inclinaison |
| Commande de secours (capteur absent) | Joystick virtuel à l'écran |

Déplacement de l'explorateur : joystick à l'écran, ou ZQSD / flèches dans l'éditeur.

## Fichiers du module

- `Assets/LostTemple/Modules/1_Accelerometre/Scripts/TiltBallPuzzle.cs`
- `Assets/LostTemple/Modules/1_Accelerometre/Scripts/HoleTrigger.cs`
- `Assets/LostTemple/Modules/1_Accelerometre/Editor/Room1_Builder.cs`
- `Assets/LostTemple/Core/Scripts/Sensors/SensorManager.cs → GetTilt(), CalibrateTilt()`

## Regénérer les scènes

Menu **Lost Temple → Générer toutes les scènes** : recrée le menu et la ou les salles de ce module (écrase les scènes existantes).

## Android

Installer le module **Android Build Support** dans Unity Hub, puis File → Build Profiles → Android → Build.
