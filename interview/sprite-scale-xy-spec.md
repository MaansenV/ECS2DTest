# aktuell hat @Packages\com.ecs2d.renderer\Runtime\SpriteData.cs nur basescale, ich möchte die option einfügen für x,y die scale unterschiedlich einzustellen

## Current spec

Die Anforderung ist jetzt ausreichend geklärt: `SpriteData` soll durchgängig echte X/Y-Skalierung unterstützen, mit neuem `float2 BaseScaleXY`, das mit Transform-X/Y multipliziert wird. Negative Transform-Skalen bleiben reine Flip-Signale. `BaseScale` bleibt als kompatibler Simple-Modus erhalten, `BaseScaleXY` hat Vorrang, der Inspector bekommt einen manuellen Simple/Advanced-Toggle, und wenn Legacy- und XY-Pfad gemischt werden, soll es eine explizite Warning geben. Das Verhalten gilt konsistent für normale Sprites und Partikel; bei Partikeln multipliziert die bestehende skalare Scale-Kurve weiterhin beide Achsen gleich.

## Q&A history

Q: Soll die neue X/Y-Skalierung primär aus der GameObject/Entity-Transform-Skalierung kommen oder als eigene Sprite-Einstellung in `SpriteData` steuerbar sein?
A: Beides: Basis in SpriteData, multipliziert mit Transform X/Y

Q: Wie soll die bestehende `BaseScale`-API behandelt werden?
A: Behalten und intern auf X=Y abbilden

Q: Soll die neue X/Y-Basisskalierung auch direkt im `SpriteDataAuthoring`-Inspector sichtbar/einstellbar sein?
A: Beides: Inspector plus Runtime/API

Q: Wie sollen negative Transform-Skalen behandelt werden, wenn X/Y jetzt getrennt skaliert werden können?
A: Wie bisher: Vorzeichen nur für Flip, Betrag für Scale

Q: Welche öffentliche API-Form willst du bevorzugen?
A: Ein neues `float2 BaseScaleXY` plus altes `BaseScale` als Kompatibilität

Q: Soll die X/Y-Skalierung nur für normale Sprites gelten oder auch für Partikel, die ebenfalls `SpriteData` verwenden?
A: Für Sprites und Partikel konsistent

Q: Wenn sowohl `BaseScale` als auch `BaseScaleXY` gesetzt sind: welches Feld soll maßgeblich sein?
A: `BaseScaleXY` hat Vorrang

Q: Wie soll das im Inspector aussehen, damit bestehende Prefabs sauber migrieren?
A: `BaseScale` als einfacher Modus, `BaseScaleXY` als erweiterter Modus

Q: Soll die Runtime/der Renderer ebenfalls echte 2D-Skalierung bekommen, also z. B. `ScaleXY` bis in den Draw-Pfad statt nur eines einzelnen `Scale`-Werts?
A: Ja, durchgehend echte X/Y-Skalierung

Q: Wie soll der Inspector zwischen einfachem und erweitertem Modus umschalten?
A: Manueller Toggle: Simple vs Advanced

Q: Bei Partikeln: Soll die bestehende skalare Scale-Kurve weiterhin beide Achsen gleich multiplizieren, oder willst du später auch getrennte X/Y-Kurven?
A: Bestehende skalare Kurve multipliziert beide Achsen gleich

Q: Willst du bei Nutzung des Legacy-Pfads (`BaseScale` ohne Advanced/XY) irgendeine Migration-Hilfe?
A: Explizite Warning wenn Legacy und XY gemischt werden
