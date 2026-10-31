# Lost Temple — Module 3 : Caméra

Projet Unity **autonome** du module 3. Il contient le code commun (`Assets/LostTemple/Core`) et uniquement ce module (`Assets/LostTemple/Modules/3_Camera`). Il se teste seul, sans les autres modules.

## Contenu

**Salle finale — L'esprit Coyote**

- 3 missions photo dans le monde réel (rouge, bleu, vert, jaune, très lumineux, très sombre, plein de détails), tirées au hasard.
- **Nouveau :** scanner en direct dans le viseur — il affiche le % de la couleur demandée avant la photo.
- Mot de passe KA-YO-TÉ à répéter, boss final (lumière / inclinaison / voix), clé dorée, porte de l'artefact.

## Ouvrir et tester

1. Unity Hub → **Add project from disk** → choisir ce dossier. Version : **Unity 6.6 (6000.6.5f1)**.
2. Ouvrir `Assets/LostTemple/Core/Scenes/MainMenu.unity` et appuyer sur **Play**.
3. **Jouer** pour la partie normale, ou **Mode démo** pour arriver directement sur chaque énigme (pas de dégâts, bouton « Salle suivante »).
4. Le menu **Commandes / Capteurs** affiche si le capteur est détecté et sa valeur en direct.

| | |
|---|---|
| Geste sur téléphone | Visez un objet de la bonne couleur pour qu'il remplisse le cadre doré, puis « Prendre la photo ». La photo est analysée puis oubliée (rien n'est enregistré). |
| Dans l'éditeur | Webcam du Mac (si autorisée dans Réglages Système → Confidentialité → Caméra) |
| Commande de secours (capteur absent) | Boutons de couleur (photo simulée) |

Déplacement de l'explorateur : joystick à l'écran, ou ZQSD / flèches dans l'éditeur.

## Fichiers du module

- `Assets/LostTemple/Modules/3_Camera/Scripts/PhotoPuzzle.cs`
- `Assets/LostTemple/Modules/3_Camera/Scripts/PhotoCameraUI.cs`
- `Assets/LostTemple/Modules/3_Camera/Scripts/PasswordPuzzle.cs`
- `Assets/LostTemple/Modules/3_Camera/Scripts/BossFightPuzzle.cs`
- `Assets/LostTemple/Modules/3_Camera/Editor/Room7_Builder.cs`

## Regénérer les scènes

Menu **Lost Temple → Générer toutes les scènes** : recrée le menu et la ou les salles de ce module (écrase les scènes existantes).

## Android

Installer le module **Android Build Support** dans Unity Hub, puis File → Build Profiles → Android → Build.
