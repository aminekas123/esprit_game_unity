# Lost Temple — Module 4 : Boussole (magnétomètre)

Projet Unity **autonome** du module 4. Il contient le code commun (`Assets/LostTemple/Core`) et uniquement ce module (`Assets/LostTemple/Modules/4_Boussole`). Il se teste seul, sans les autres modules.

## Contenu

**Salle 2 — Le labyrinthe**

Quatre épreuves, toutes avec le **magnétomètre** :

1. **L'aiguille magique** — trouver 3 directions successives (une lampe s'allume à chacune) ; le passage secret s'ouvre.
2. **La rose des vents** — se tourner vers les **vrais** points cardinaux (Nord, Est, Sud, Ouest) demandés au hasard. Le « N » rouge de la rose pointe vers le nord magnétique réel.
3. **Le coffre aimanté** — le magnétomètre mesure aussi la **force** du champ magnétique : approcher le téléphone d'un objet métallique réel (clé, ciseaux, ordinateur, aimant) fait monter le champ et ouvre le coffre.
4. **La luciole** — une luciole avance toute seule dans un labyrinthe au sol ; on la dirige en tournant sur soi-même.

Plus : mini-labyrinthe à l'entrée et jets de feu avant la porte.

## Ouvrir et tester

1. Unity Hub → **Add project from disk** → choisir ce dossier. Version : **Unity 6.6 (6000.6.5f1)**.
2. Ouvrir `Assets/LostTemple/Core/Scenes/MainMenu.unity` et appuyer sur **Play**.
3. **Jouer** pour la partie normale, ou **Mode démo** pour arriver directement sur chaque énigme (pas de dégâts, bouton « Salle suivante »).
4. Le menu **Commandes / Capteurs** affiche si le capteur est détecté et sa valeur en direct.

| | |
|---|---|
| Geste sur téléphone | Tenez le téléphone à plat ou droit et tournez sur vous-même jusqu'à ce que l'aiguille pointe vers le fond de la salle. Évitez les aimants et objets métalliques. |
| Dans l'éditeur | A / E = tourner, M = approcher un aimant (coffre) |
| Commande de secours (capteur absent) | Boutons ◀ Gauche / Droite ▶, bouton « Approcher l'aimant » |

Déplacement de l'explorateur : joystick à l'écran, ou ZQSD / flèches dans l'éditeur.

## Fichiers du module

- `Assets/LostTemple/Modules/4_Boussole/Scripts/CompassPuzzle.cs`
- `Assets/LostTemple/Modules/4_Boussole/Scripts/CardinalPuzzle.cs`
- `Assets/LostTemple/Modules/4_Boussole/Scripts/MagnetChestPuzzle.cs`
- `Assets/LostTemple/Modules/4_Boussole/Scripts/FireflyMazePuzzle.cs`
- `Assets/LostTemple/Modules/4_Boussole/Editor/Room2_Builder.cs`
- `Assets/LostTemple/Core/Scripts/Sensors/SensorManager.cs → ComputeHeading(), GetHeading()`

## Regénérer les scènes

Menu **Lost Temple → Générer toutes les scènes** : recrée le menu et la ou les salles de ce module (écrase les scènes existantes).

## Android

Installer le module **Android Build Support** dans Unity Hub, puis File → Build Profiles → Android → Build.
