# Lost Temple — L'éveil des capteurs

Jeu mobile Unity (Android) réalisé en équipe de 6 : un explorateur traverse un temple perdu, et **chaque salle se résout avec un capteur du téléphone**. Accéléromètre, gyroscope, boussole, micro, capteur de lumière, capteur de proximité et caméra : chaque épreuve utilise le vrai matériel du téléphone.

Cette branche (`main`) contient le **jeu complet**. La branche `modules` contient le même jeu découpé en **6 projets Unity autonomes**, un par capteur, pour que chaque membre teste son module seul.

## Les salles

| # | Salle | Capteur | Épreuves |
|---|---|---|---|
| 1 | Le pont suspendu | Accéléromètre | Guider la boule d'or en inclinant le téléphone sur un parcours en S, et percuter la boule rouge (NavMesh Agent) avant qu'elle atteigne votre départ ; plateau à trous |
| 2 | Le labyrinthe | Boussole | Trouver 3 directions, la rose des vents, le coffre aimanté (champ magnétique), la luciole poursuivie par une boule rouge chasseuse (NavMesh Agent) |
| 3 | Les statues | Gyroscope | Orienter 3 statues, guider un rayon par 3 miroirs jusqu'au cristal, « Jacques a dit » (tourner dans le sens affiché), puis fuir le rocher |
| 4 | Le monstre | Microphone | Réveiller le monstre, pousser la boule d'énergie à la voix, lire une phrase en anglais (reconnaissance vocale Android) |
| 5 | La cachette | Proximité + luminosité | Activer 3 runes en couvrant le capteur, échapper aux momies ; luminosité du jeu inversée (lumière forte → jeu sombre, obscurité → jeu au maximum) |
| 6 | Le brasier | Luminosité | Éteindre un grand brasier en couvrant le capteur de lumière 5 secondes |
| 7 | L'esprit Coyote | Caméra | Photographier des objets de la couleur demandée, retrouver le mot de passe, ouvrir la porte avec la clé |
| 8 | Le décor | Caméra | Scanner librement des objets : chaque couleur détectée devient un objet 3D dans la salle |

Chaque épreuve a aussi des **commandes de secours** (boutons à l'écran, ou clavier dans l'éditeur) si le capteur est absent ou refusé.

## Ouvrir et lancer le projet

1. Installer **Unity 6.6 (6000.6.5f1)** avec le module **Android Build Support**.
2. Unity Hub → **Add project from disk** → choisir ce dossier.
3. Menu **Lost Temple → Générer toutes les scènes** : construit le menu et les salles, et remplit les Build Settings.
4. Ouvrir `Assets/LostTemple/Core/Scenes/MainMenu.unity` → **Play**.

**Commandes dans l'éditeur :** flèches / ZQSD = inclinaison, A / E = rotation, Espace = souffle, C = couvrir le capteur de proximité, L = lampe, K = cacher le capteur de lumière, R = secousse.

**Mode démo (soutenance)** dans le menu : 9 vies, un bouton « Salle suivante » pour passer directement d'une salle à l'autre, et l'explorateur placé directement sur l'épreuve.

## Construire l'APK Android

Dans Unity : menu **Lost Temple → Construire l'APK Android** (IL2CPP, ARM64). Le fichier est créé dans `Builds/` (non versionné).

Sans ouvrir Unity :

```bash
/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath . -buildTarget Android -executeMethod AndroidBuild.Build
```

Puis installer sur un téléphone branché en USB (débogage USB activé) :

```bash
adb install -r Builds/Lost_Temple.apk
```

## Organisation du code

```
Assets/LostTemple/
├── Core/                  Code commun, indépendant des modules
│   ├── Scripts/Core/      GameManager, RoomManager (enchaîne les épreuves), CameraRig, Journal
│   ├── Scripts/Sensors/   SensorManager : lecture de tous les capteurs + commandes de secours
│   ├── Scripts/Player/    Explorateur (CharacterController), animation, lanterne
│   ├── Scripts/Puzzles/   PuzzleStep (classe de base d'une épreuve), Station, pièges
│   ├── Scripts/UI/        Interface construite par code (HUD, fenêtres, jauges)
│   └── Editor/            Générateur de scènes, captures des salles
└── Modules/               Un dossier par capteur (scripts, générateur de salle, scènes)
    ├── 1_Accelerometre/
    ├── 2_Gyroscope/
    ├── 3_Camera/
    ├── 4_Boussole/
    ├── 5_Micro/
    └── 6_Luminosite/
```

- **Une épreuve** = un script qui hérite de `PuzzleStep`. Le `RoomManager` de la salle les enchaîne : station (faisceau doré) → tutoriel → épreuve → porte.
- **Une salle** est construite par code dans `Modules/<module>/Editor/` (méthode marquée `[RoomBuilder(ordre, scène, module)]`). L'ordre des salles du jeu vient de cet attribut.
- **Scènes faites à la main** : une scène retouchée dans l'éditeur est listée dans `Modules/<module>/Scenes/HandEdited.txt`. Le générateur ne l'écrase pas et ajoute au jeu celles qui n'ont pas de générateur (c'est le cas des salles 7 et 8 de la Caméra).
- Le Core ne connaît pas les modules : on peut retirer un module, le reste du jeu fonctionne.

## Équipe et modules

| Module | Capteur |
|---|---|
| 1 | Accéléromètre |
| 2 | Gyroscope |
| 3 | Caméra |
| 4 | Boussole |
| 5 | Microphone |
| 6 | Luminosité (+ proximité) |

## Confidentialité

Le micro mesure seulement le volume, ou passe par la reconnaissance vocale d'Android pour la phrase en anglais. Les images de la caméra sont analysées sur le téléphone puis oubliées. **Rien n'est enregistré ni envoyé.**
