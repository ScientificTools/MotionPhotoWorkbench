MotionPhotoWorkbench V1.2.1

Nouveautés de cette version :
- correction du crash SplitterDistance au démarrage
- mise en page 3 zones plus stable (frames / image / outils)
- tailles minimales des panneaux configurées
- SplitterDistance calculé de façon sûre au Load / Shown / Resize
- boutons de navigation plus lisibles

Notes :
- placer ffmpeg.exe à côté de l'exécutable compilé
- certains fichiers .jpg affichés comme Motion Photo par Windows peuvent ne plus contenir la vidéo embarquée si le fichier a été copié/exporté autrement ; dans ce cas, l'application ne pourra extraire qu'une image
- cette archive n'a pas pu être compilée dans cet environnement, car le SDK .NET n'y est pas installé


V1.2.3 corrections:
- SplitContainer BeginInit/EndInit removed to avoid SplitterDistance crash during designer initialization.
- Explicit using directives added so the project compiles even if implicit usings are not active in the IDE/project cache.
