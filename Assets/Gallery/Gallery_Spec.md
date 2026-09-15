# Unity Virtual Gallery – Spec: Wände unsichtbar & Bildplatzierung

**Rolle:** Christian = Product Owner, Claude = Projektmanager, "Code" = Umsetzung im Unity-Projekt.
**Projekt auf der Festplatte:** `D:\Unity\Gallery_DPUnityPlugin`
**Status:** ✅ Geklärt, bereit zur Umsetzung durch "Code"
**Stand:** 2026-09-15

## Kontext
Das Unity-Projekt basiert auf dem **domeprojection.com Unity Plugin (DPUnityPlugin)** – einer lizenzierten Warp/Blend/Blacklevel-Korrektur-Lösung für Mehrprojektor-Setups. Das heißt: dies ist keine reine "Walkthrough-Demo", sondern eine Vorschau/Content-Szene für eine **reale Multi-Beamer-Installation** (6 Kanäle/Projektoren, siehe `correction/config.xml`). Die Wände sollen deshalb im Rendering leer/unsichtbar bleiben – die Bilder sind der eigentliche projizierte Inhalt, die Wandgeometrie dient nur der korrekten 3D-Ausrichtung/Verzerrungskorrektur.

**Unity-Version:** 2018.4.36f1 (Built-in Render Pipeline, kein URP/HDRP → Shader-Properties sind `_MainTex`, nicht `_BaseMap`).

## Ist-Zustand im Projekt (per Analyse festgestellt)

**Szenen** (`Assets/Scenes/`): 5 Szenen, nur `SampleScene_3d.unity` ist relevant (Raum-Modell "Lernraum" platziert, Main Camera mit `dpCorrection`-Script, `correction/config.xml`, 6 Kanäle, Type 0 = statisches 3D-Warping). Die anderen 4 Szenen sind unveränderte Plugin-Demos und werden nicht angefasst.

**Raum-Modell** (`Assets/Gallery/Lernraum.fbx`) enthält:
- `Boden` (Material `Boden_Mat`)
- `Wand_Hinten`, `Wand_Links`, `Wand_Rechts`, `Wand_Vorne` – 4 Wände, gemeinsames Material `Wand_Mat`
- `Deckenlicht` – Licht, keine Geometrie; keine separate Decke im Modell
- `Gemälde_01` … `Gemälde_06` – **6 bereits fest im Modell platzierte, rahmenlose Bildflächen**, je eigenes Material

**Collider:** Aktuell **keine Collider** irgendwo im Modell (`addColliders: 0` im FBX-Import) – müssen für Wände UND Boden aktiv ergänzt werden.

**Bild-Assets** (`Assets/Gallery/Pictures/`): `Blumen.png`, `feld.png`, `Les_mangeurs_de_pommes_de_terre.jpg`, `Nacht_1.png`, `Nacht_2.png`, `Vase.png` – 6 Bilder für 6 Slots, Zuordnung ist frei wählbar.

**Nicht relevant für die Gallery:** `Assets/Dodekaeder_4k_v2.png`, `Assets/mapping_0.obj`, `Assets/ProjectionMapping.mat` (gehören zur "PM"-Demoszene des Plugins).

**Tags/Layer:** Keine eigenen Tags definiert bisher. Layer 8 = `"PM"`, vom Plugin reserviert – nicht anfassen.

## Klärungen mit Christian
1. `Nacht_1.png` / `Nacht_2.png` sind zwei unterschiedliche, gültige Bilder (von Christian umbenannt) – keine Dublette, beide werden verwendet.
2. Zuordnung Bild → `Gemälde_0X`-Slot ist **frei** wählbar, nicht fachlich vorgegeben.
3. Die `Gemälde_0X`-Objekte haben **keinen Rahmen** – muss neu gebaut werden.
4. Der **Boden bekommt ebenfalls einen Collider** (angelegt für begehbaren Rundgang / Charakter-Controller).

---

## Grundsatzentscheidungen
1. **Wände:** unsichtbar, Kollision bleibt (Collider muss ergänzt werden).
2. **Boden:** bleibt sichtbar (nur Wände werden ausgeblendet), bekommt zusätzlich einen Collider.
3. **Bildquelle (v1):** feste `Texture2D`-Referenzen im Inspector, die 6 vorhandenen Bilder.
4. **Platzierung:** kein Algorithmus nötig – 6 Slots (`Gemälde_01`…`_06`) sind im Modell bereits fest positioniert.
5. **Zuordnung Bild → Slot:** im Inspector frei editierbar (keine Hardcodierung), Default-Vorschlag siehe Paket 3.
6. **Präsentation:** Bild + Rahmen – Rahmen muss neu gebaut werden.

---

## Aufgabenpakete für "Code"

### Paket 1 – Collider ergänzen
- An `Wand_Hinten`, `Wand_Links`, `Wand_Rechts`, `Wand_Vorne`: je einen Collider hinzufügen (`MeshCollider`, nicht-convex reicht da statisch; alternativ `BoxCollider`, passend zur Wandausrichtung skaliert).
- An `Boden`: ebenfalls einen Collider hinzufügen (`MeshCollider` reicht für eine ebene Fläche).
- Umsetzung z.B. direkt als Komponenten im Prefab/in der Szene, oder per Script beim Start (`AddComponent<MeshCollider>()` für die 5 genannten Objekte). Kein FBX-Import-Setting global ändern (das würde unnötig auch an den `Gemälde_0X`-Objekten Collider erzeugen).

### Paket 2 – Wände unsichtbar machen
- Script `WallVisibility.cs`: beim Start für `Wand_Hinten/Links/Rechts/Vorne` → `GetComponent<Renderer>().enabled = false`. Collider aus Paket 1 bleibt aktiv.
- Objekte direkt per Name referenzieren oder neuen Tag `"Wall"` anlegen und zuweisen.
- Boden bleibt sichtbar – nicht anfassen.

### Paket 3 – Bilder zuweisen
- `GalleryManager.cs`: Inspector-Liste `(string gemäldeObjectName, Texture2D artwork)[]`, Default-Vorschlag:
  1. `Gemälde_01` → `Blumen`
  2. `Gemälde_02` → `feld`
  3. `Gemälde_03` → `Les_mangeurs_de_pommes_de_terre`
  4. `Gemälde_04` → `Nacht_1`
  5. `Gemälde_05` → `Nacht_2`
  6. `Gemälde_06` → `Vase`
- Pro Eintrag: Objekt per Name finden, Material-Instanz holen, `_MainTex` = zugewiesene Textur setzen.
- Prüfen: Seitenverhältnis Bild vs. Mesh-Größe – bei Abweichung ggf. Mesh-Skalierung anpassen oder Bild croppen/lettern (visuell im Editor entscheiden).

### Paket 4 – Rahmen ergänzen
- Pro `Gemälde_0X`: zusätzliches Kind-Objekt mit einfachem Rahmen-Mesh (z.B. 4 flache Boxen um die Kanten, oder ein Rahmen-Prefab), passend zur Größe des jeweiligen Bildes skaliert/positioniert. Original-Bildfläche bleibt unverändert.

---

## Abnahmekriterien
- [ ] Wände sind im Play-Mode unsichtbar, Collider blockiert weiterhin (Wände + Boden).
- [ ] Alle 6 `Gemälde_0X`-Objekte zeigen das zugewiesene Bild unverzerrt.
- [ ] Jedes Bild hat einen sichtbaren Rahmen.
- [ ] Zuordnung Bild → Slot ist im Inspector änderbar, ohne Code-Änderung.
- [ ] Kein Eingriff in `Packages/DPUnityPlugin` oder `correction/`.
- [ ] Die anderen Demo-Szenen (`_2d`, `_dw`, `_pm`, `_pm dw`) bleiben unverändert.

## Spätere Erweiterung (nicht Teil von v1)
- Dynamisches Laden von Bildern aus Ordner/URL statt fester Texture2D-Liste.
- Info-Schilder mit Titel/Künstler/Jahr.
- Begehbarer Rundgang mit Charakter-Controller (Boden-Collider ist dafür bereits vorbereitet).

## Nächste Schritte
Alle offenen Fragen sind geklärt – die Spec ist bereit, paketweise an "Code" übergeben zu werden: erst Paket 1+2 (Collider + Sichtbarkeit), dann Paket 3 (Bilder), dann Paket 4 (Rahmen). Nach jedem Paket Review im Unity-Editor durch Christian.
