# Lost Temple — Module 2 : Gyroscope

Projet Unity **autonome** du module 2. Il contient le code commun (`Assets/LostTemple/Core`) et uniquement ce module (`Assets/LostTemple/Modules/2_Gyroscope`). Il se teste seul, sans les autres modules.

## Contenu

**Salle 3 — Les statues**

- Orienter 3 statues vers leur symbole en tournant le téléphone comme un volant.
- **Nouveau :** miroirs à orienter pour guider un rayon de lumière jusqu'au cristal (bouton « Miroir suivant »).
- À la fin, un rocher géant poursuit l'explorateur jusqu'à la porte.

## Ouvrir et tester

1. Unity Hub → **Add project from disk** → choisir ce dossier. Version : **Unity 6.6 (6000.6.5f1)**.
2. Ouvrir `Assets/LostTemple/Core/Scenes/MainMenu.unity` et appuyer sur **Play**.
3. **Jouer** pour la partie normale, ou **Mode démo** pour arriver directement sur chaque énigme (pas de dégâts, bouton « Salle suivante »).
4. Le menu **Commandes / Capteurs** affiche si le capteur est détecté et sa valeur en direct.

| | |
|---|---|
| Geste sur téléphone | Faites pivoter le téléphone comme un volant, sans le pencher vers l'avant. |
| Dans l'éditeur | A / E = tourner, N = miroir suivant |
| Commande de secours (capteur absent) | Boutons ↺ / ↻ et « Miroir suivant » |

Déplacement de l'explorateur : joystick à l'écran, ou ZQSD / flèches dans l'éditeur.

## Fichiers du module

- `Assets/LostTemple/Modules/2_Gyroscope/Scripts/StatueAlignPuzzle.cs`
- `Assets/LostTemple/Modules/2_Gyroscope/Scripts/MirrorBeamPuzzle.cs`
- `Assets/LostTemple/Modules/2_Gyroscope/Scripts/Boulder.cs`
- `Assets/LostTemple/Modules/2_Gyroscope/Editor/Room3_Builder.cs`
- `Assets/LostTemple/Core/Scripts/Sensors/SensorManager.cs → GetRotationRate()`

## Regénérer les scènes

Menu **Lost Temple → Générer toutes les scènes** : recrée le menu et la ou les salles de ce module (écrase les scènes existantes).

## Android

Installer le module **Android Build Support** dans Unity Hub, puis File → Build Profiles → Android → Build.
