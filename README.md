# MotionPhotoWorkbench

**Que fait cet outil ?**
Il a pour but de retraiter une video courte, ou une photo animée (smartphone, appareil photo prenant en rafale entre 20 et 80 images ), en effectuant des traitements image par image, avant de reconstituer une video dans un format d'export standard, **léger, et adapté aux pages Web** (webM, webP, Mpeg). Vous pourrez choisir les photos à garder, stabiliser l'animation avec un système d'ancrage automatique, appliquer une colorimétrie à l'ensemble des images, cropper le résultat dans un rectangle aux ratios choisis, définir une vitesse de défilement en img/s et contrôler le résultat puis l'exporter dans un format d'export standard, léger, exploitable directement dans une page WEB (webM, Mpeg, webP, gif animé) ou lecteur video (webM, Mpeg).

**Exemple** : Image animée source provenant d'un Pixel 9A en digiscopie sur une longue vue, **avec forte instabilité** de 3,5 Mo 

[rougeGorge_source.webm](https://github.com/user-attachments/assets/6a9e5c65-248a-49af-a273-41ece9fb6861) 


**Résultat ci dessous** : 

Vidéo après traitement, au format **WEBM** de **0,3 Mo**, avec **recentrage automatique des images**, **colorimétrie**, et **sélection de la zone d'export au ratio 4/3**. Ce traitement a été fait avec l'option **round trip (yoyo)** qui double les images avec la séquence avant, puis la même séquence en sens inverse, afin que la dernière image revienne sur la première. **L'option "une image sur deux"** a permis d'avoir le même nombre d'image que la séquence normale afin de garder la même taille (à peu près). La **vitesse de défilement**, par défaut à 20 images par secondes a été diminuée à 10 pour avoir le même rendu des mouvements.

[RougeGorge__stabilise_roundTrip_1ImageSur2_colorimetrie.webm](https://github.com/user-attachments/assets/bb597a6b-fac5-4b98-9630-c5bc78e0f8cc)

**Qu'est-ce qu'une photo animée** ?
Simple ! Depuis le smartphone (ou un appareil photo adapté), on prend une photo comme d'habitude. L'appareil a déjà mémorisé 1s de video avant que vous ne déclenchiez, puis il continue encore 1s après, ce qui réalise une petite video courte de 20 à 80 images (en fonction de sa capacité à digérer votre fréquence de déclenchements).

**A quoi ça sert ? Qu' est-ce que ça apporte ?**
On peut se le demander... Lorsque cette image video sort du smartphone, c'est un fichier .JPG, qui contient, par une astuce d'encodage maison, une photo fixe, et un fichier MPEG à la queue leu leu dans le même fichier. 

Rares sont les logiciels qui savent exploiter la video incrustée à la fin du fichier. Ils ne savent restituer que l'image simple. Les pages WEB ne savent pas non plus les exploiter autrement qu'en visualisant l'image simple.
 
Donc à quoi ça sert ? Pas grand chose en l'état... On est condamné à consulter l'animation depuis son smartphone ou appareil photo. Ca sert à pouvoir  choisir la meilleure photo de la rafale, mais pas à exploiter la video.

Pourtant, ces animations courtes apportent de la vie et du relief à une photo. Elles n'ont pas non plus la lourdeur des videos de 10 min.
En ornithologie, les oiseaux étant constamment en mouvement, 1s d'animation, c'est déjà un régal, une tranche de vie, comparé à une photo fixe, plastique, graphique, mais figée, comme empaillée.

**Quelles sont les difficultés ?**
En parlant d'ornithologie, les photos sont prises avec une longue vue, et un smartphone peu onéreux (digiscopie), ou un appareil photo avec un téléobjectif puissant (400mm, 600mm, 800mm). Avec le grossissement important, il est impossible de ne pas trembler ni garder le sujet bien en place. Il faut avoir un trépied, et/ou au moins une optique stabilisée. Mais même là, une vidéo a du mal à garder le sujet centré sur le lapse de temps. Plutôt bien avec un trépied (attention à ne rien faire bouger au moment du déclenchement), encore possible avec une optique stabilisée à main levée, impossible sans stabilisation.

Possesseur d'une longue vue stabilisée (par flemme de me déplacer avec un trépied), et d'un smartphone pour faire de la digiscopie, j'ai essayé les photos animées, et confronté à l'absence d'outil pour les retravailler, je me suis lancé dans l'aventure de créer cet outil encore manquant. Je l'utilise pour moi, et le met à disposition sans contrainte de license, ni de code source pour tous ceux qui voudraient tenter l'aventure. 
MotionPhotoWorkBench est né avec des moyens limités : je n'ai pas de MAC, je tourne sous Windows, donc il n'est ciblé que Windows pour l'instant. Si ce projet arrive à intéresser une communauté, il grandira, mais pour l'instant, il a cette limite.

**Comment ça marche**

Ce programme se base sur **FFMPEG** https://github.com/ffmpeg/ffmpeg (libre de droit, très répandu, efficace, merci aux concepteurs) pour **décomposer la video en images individuelles** et **recompose les images travaillées par MotionPhotoWorkbench en vidéo**.
Le programme offre les commandes pour **recentrer automatiquement chaque image sur un point d'ancrage** (stabilisation), **aligne et recoupe automatiquement les images sur ce point d'ancrage**. On peut **écarter des images** de mauvaise qualité, appliquer un **réglage colorimétrique** sur toutes les images. Une **prévisualisation en image fixe, avec les mouvements en transparence** vous permet de choisir une **fenêtre de crop** en précisant le **ration X/Y**. Puis l'export final est assuré par FFMPEG, dans un format auchoix **Mpeg, WEBM, WEBP, GIF animé** (lourd), avec possibilité de **prévisualiser la video** depuis l'application, avec **indication de sa taille**. Le pilotage de FFMPEG est transparent. L'ensemble des manipulations est assez rapide (5 à 20 min), et peut être **sauvegardé dans un projet** (hors frames individuelles qui seront re-extraites en cas de reprise du projet). Le résultat, léger en taille est exploitable dans une simple balise HTML. **Les images sources sont évidemment préservées**, **le traitement est réalisé dans un répertoire temporaire, et le programme vous indique avant sa taille probable**, en fonction de la source, et de l'espace disque disponible.

**Quelles sources d'images peut-il traiter ?**
plusieurs sources variées : 
- un répertoire avec des photos ordonnées déjà extraites (peu importe leur conventions de noms ou leur format, elles seront prises par ordre alphabétique) : on a ici coupé le passage de la vidéo aux images individuelles pour pouvoir traiter les rafales de photos déjà disponibles en images individuelles
- des vidéos d'un peu tous les formats standards - mais courtes s'il vous plait, avec 2s, vous aurez besoin d'à peu près 150Mo de disque pour le traitement, alors pour 1 min, ça fait... 4,5 Go ?
- des images animées sortant de smartphones tels que google Pixel, Samsung, iPhone, avec leurs formats propriétaires : une image + une video à la suite dans le même fichier.

**attention**, en l'état, limité par mon budget smartphone, j'ai utilisé le seul que j'avais, un google Pixel (9A qui a la bonne idée de ne posséder que 2 objectifs, ce qui est pratique en digiscopie). Ne disposant pas de Samsung ou iPhone, je suis preneur de vos retours, et si le programme ne les reconnait pas, n'hésitez pas à m'envoyez des exemples sources de photos, je me ferai fort des les rendre compatibles - c'est un peu le but.

**Question de taille**

[RougeGorge__stabilise_roundTrip_1ImageSur2_colorimetrie.webm](https://github.com/user-attachments/assets/bb597a6b-fac5-4b98-9630-c5bc78e0f8cc)

- La taille de **0,3 Mo** en WebM est la meilleure option, écologique, presque étonnante comparée à une image simple. 
- **la source image animée** sortie du pixel 9a) : **3,5 Mo**
- **le MPEG brut extrait** par MotionPhotoWorkBench : **2,3 Mo** : contient **45 images**
- **Un WEBM équivalent au MPEG** (nombre et taille d'images) : **0,5 Mo**
- pour le traitement, les frames individuelles sont travaillées en PNG. ici, **chaque PNG fait 1Mo**
- le traitement nécessite 3 répertoires temporaires : 
     - frames : les 45 frames de départ en PNG
     - final : les frames de travail, visualisées avec la colorimétrie, ne contient pas les images écartées. Chaque changement de colorimétrie repart des frames initiales (frames) : pas d'images supprimées => 45 images aussi
     - aligned : les images centrées et découpées pour se superposer parfaitement, prêtes pour être fusionnées en vidéo. 45 images ici.
- la **taille du répertoire temporaire sera donc ici de l'ordre de 140Mo** : 1 Mo (PNG) * 45 (nb images) * 3 (répertoires) + 2,3 Mo video MPEG extraite de l'image animée. Ce répertoire temporaire vit le temps du traitement et peut être supprimé à tout moment, auquel cas il sera recalculé à partir de l'image ou répertoire source

**Qualité des images**

Il n'y a pas de magie, une photo fixe peu compressée apportera toujours plus de piqué et de détails, avec cet effet Wahoo, et la possibilité de l'imprimer grand format.
Mais pour une visualisation écran, la photo animée apporte ce petit plus de vie et de relief : son but n'est pas de pouvoir être imprimée, mais d'illustrer un moment de vie et de comportement, tout en restant écologique en consultation.
 
Les photos graphiques et les vidéos documentaires ont leur propre intérêt et cibles. Les photos animées naviguent entre les deux, ni photo, ni video, mais les deux à la fois; leur plus, c'est la vie, le comportement du sujet et le relief, l'instant capturé et pétillant.

Pour le rapport taille/qualité, le format WEBM est bluffant, presque écologique. Mes résultats oscillent entre 300Ko et 1Mo, rarement plus, en moyenne 500Ko, donc pas de honte à les présenter sur des sites WEB.


PUB (mais sans intérêt personnel), vous pouvez voir plus d'exemples de résultats sur mon site de balade ornithologique et nature en ville : https://www.baladechampvert.fr


## Code License

Le code est sous license MIT, donc libre de récupération et adaptation. Voir : [LICENSE.txt](LICENSE.txt).
