# ich möchte nun editor windows erstellen, damit das erstllen von spritesheets und animationset einfacher geht, sowas wie ein wizard, auch soll es ein window geben das alle spritesheets scritable anzeigt, damit man dann leicht sieht welche id man gegeben hat.

## Current spec

Das MVP ist jetzt weitgehend definiert: Im UPM-Paket `com.ecs2d.renderer` sollen zwei minimale Unity-Editor-Tools entstehen. Der Wizard soll in 2 bis 3 Schritten mit Next/Back durch das Anlegen oder Aktualisieren von genau einem SpriteSheet plus einem Animationsset führen, sowohl importierte Sprites als auch manuelle Eingaben unterstützen, Animationen per Namensmuster vorschlagen und nachbearbeitbar machen, eine kleine Sprite-Preview zeigen, Namenskonventionen vorschlagen, aber nur warnen, und vor dem Speichern möglichst vollständig validieren. Bei bestehender ID soll ein vorhandenes Asset zum Aktualisieren angeboten werden, wobei das Update-Verhalten pro Bereich auswählbar ist. Das Übersichtsfenster soll zunächst read-only bleiben, alle SpriteSheet-ScriptableObjects projektweit automatisch finden, nach ID/Name/Pfad filterbar sein und direkt ID, Name, Pfad sowie Sprite- und Animation-Anzahl anzeigen; Aktionen sind Öffnen und ID-Kopieren. Beide Tools sollen über Menüeinträge und Kontextmenüs auf relevanten Assets inklusive Sprites erreichbar sein.

## Q&A history

Q: Welche Aufgaben soll der erste Wizard konkret abdecken?
A: SpriteSheet + Animationsset anlegen

Q: Woher sollen die SpriteSheets im Übersichtsfenster kommen?
A: zuerst automatisch suchen, später filterbar

Q: Welche Eingaben soll der Wizard im ersten Schritt unterstützen?
A: beides unterstützen

Q: Wie soll die SpriteSheet-ID vergeben werden?
A: automatisch vorschlagen, manuell änderbar

Q: Welche Assets soll der Wizard am Ende tatsächlich erzeugen oder aktualisieren?
A: neu anlegen oder bestehende aktualisieren

Q: Welche Aktionen brauchst du direkt im SpriteSheet-Übersichtsfenster?
A: anzeigen + öffnen + ID kopieren

Q: Wie sollen Animationen im Wizard primär definiert werden?
A: beides, mit Auto-Vorschlag zum Nachbearbeiten

Q: Was soll passieren, wenn die vorgeschlagene oder eingegebene SpriteSheet-ID schon existiert?
A: bestehendes Asset zum Aktualisieren anbieten

Q: Für wen soll der erste Wizard primär optimiert sein?
A: beides, mit sehr geführtem Ablauf

Q: Welche Filter sind im Übersichtsfenster zuerst am wichtigsten?
A: ID + Name + Pfad

Q: Welche Prüfungen soll der Wizard vor dem Speichern zwingend machen?
A: möglichst vollständige Validierung mit klaren Fehlermeldungen

Q: Wie sollen die Tools im Editor erreichbar sein?
A: über Menü + Kontextmenü auf passenden Assets

Q: Wie soll der Wizard den Speicherort für neue Assets festlegen?
A: Standard vorschlagen, aber änderbar

Q: Soll die SpriteSheet-Übersicht anfangs nur Übersicht sein oder auch Bearbeitung erlauben?
A: erst read-only, Bearbeitung später

Q: Wenn der Wizard ein bestehendes Asset aktualisiert: soll er vorhandene Animationsdaten eher ersetzen oder vorsichtig zusammenführen?
A: vor dem Speichern pro Bereich auswählbar

Q: Was ist für die erste umsetzbare Version wichtiger?
A: beides minimal in einem ersten Schritt

Q: Sollen diese Editor-Tools direkt im UPM-Paket `com.ecs2d.renderer` liegen oder eher im Projekt unter `Assets`?
A: direkt im Paket

Q: Woran merkst du, dass das erste MVP erfolgreich ist?
A: alle drei Punkte

Q: Soll der Wizard im MVP nur einen Asset-Satz pro Durchlauf erzeugen oder auch Batch-Erstellung unterstützen?
A: erst einzeln, Batch später

Q: Welche Zusatzinfos soll die Übersicht neben ID, Name und Pfad direkt anzeigen?
A: Sprite-Anzahl und Animation-Anzahl

Q: Soll der Wizard für neue Assets feste Namenskonventionen erzwingen oder nur Vorschläge machen?
A: Konvention vorschlagen und warnen bei Abweichung

Q: Brauchst du im MVP schon eine visuelle Vorschau im Wizard?
A: kleine Sprite-Preview reicht

Q: Wie fein geführt soll der Wizard im MVP aufgebaut sein?
A: 2 bis 3 Schritte mit Next/Back

Q: Von welchen Assets soll das Kontextmenü den Wizard direkt starten können?
A: auf allen relevanten Assets inkl. Sprites
