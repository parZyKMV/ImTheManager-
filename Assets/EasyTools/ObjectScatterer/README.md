# Object Scatterer

Fill outdoor scenes with props—rocks, vegetation, clutter—using **batch scatter** or **brush painting** directly in the Scene view. The editor window holds sources and rules; tooltips explain each field.

**Requirements:** Unity **6** (Overlays / Scene toolbar integration). Editor-only; nothing is added at runtime.

---

## Quick start

1. **Window → EasyTools → Object Scatterer**
2. Under **Content**, add **Scatter Sources** (project prefabs and/or hierarchy objects). Optionally set **Parent Container** (otherwise a **Scattered Objects** root is created).
3. Under **Placement**, pick **Batch Scatter** or **Brush Paint** (see [Workflows](#workflows-batch-vs-brush)).
4. Adjust **Randomization** (scale, rotation, optional jitter) as needed.
5. **Batch:** set region + **Object Count**, then **Scatter Objects**. **Brush:** select the **Object Scatterer** Scene tool (**OS** in the tool palette / [overlay](#scene-view-unity-6)), then paint on surfaces.

---

## Workflows (Batch vs Brush)

| | **Batch Scatter** | **Brush Paint** |
|---|-------------------|----------------|
| **Best for** | Many instances at once, defined region | Art-directed strokes, stamps along a surface |
| **Placement mode** | **Ground Raycast** (on surfaces) or **Random Volume + Drop** (box + physics) — [details](#batch-scatter--placement-modes) | Uses the same raycast / filters as **Ground Raycast** (not Random Volume layout) |
| **Physics UI** | Depends on placement mode ([Physics](#physics)) | Always **Optional drop** — not tied to Random Volume options |
| **Scene tool** | Optional; batch button in window | **Required** — painting only while **Object Scatterer** is the active tool |

---

## Batch Scatter — placement modes

These options apply when **Placement → Batch Scatter** is selected.

- **Ground Raycast** — Scatter inside a **sphere** or **box** around **Scatter Center** (default world origin). Optional **Snap to surfaces in region** raycasts to colliders. Set **Scatter Radius** or box half-extents and **Object Count**.
- **Random Volume + Drop** — Random positions inside **Random Volume Half Extents**, then **mandatory** rigidbody settle. Prefer keeping the volume **above** floors so instances do not spawn inside solid geometry.

**Raycast Layers** is shown for Ground Raycast; it is hidden for Random Volume + Drop (that mode does not use placement raycasts).

---

## Scene view (Unity 6)

- **Overlays:** Enable **Object Scatterer** if the strip is hidden (**Overlays** menu on the Scene view).
- **OS** — Shortcut label on the overlay button and on the Scene **tool** palette. Activates the **Object Scatterer** editor tool and can open the settings window. **Painting only works while this tool is active**; switching to Move / Rotate / Scale stops the brush.
- The overlay mirrors brush **Radius** / **Spacing** (shown as **R** / **S** when space is tight), **Drop sim**, **Preview**, **Steps/Frame**, and **Max time**.

---

## Physics

### Batch Scatter + Random Volume + Drop

Every instance runs drop simulation up to **Max Simulate Time**. Turn **Preview** on to see motion in the Scene view over multiple editor frames. **Steps/Frame** can be changed during preview and applies on the next tick.

### Ground Raycast and Brush Paint — optional drop

**Simulate Drop On Spawn** is optional. With **Brush**, **Spawn height along normal** offsets the instance along the surface normal before the simulation. **Preview** / **Steps/Frame** / **Max time** control playback; **Steps/Frame** updates immediately during preview.

> **Brush:** Physics options here are **not** the “Random Volume only” block—that block is **Batch Scatter + Random Volume + Drop** only.

---

## Ground Raycast — how hits are resolved

Placement tries, in order: physics raycast against colliders, **TerrainCollider**, then **triangle tests** on **MeshRenderer** / **MeshFilter** and **SkinnedMeshRenderer** (meshes without colliders can still be hit). Inactive objects and child meshes are included where applicable.

Static meshes need **Read/Write** enabled on the mesh asset for triangle data. Skinned meshes are baked in the editor for tests.

If a batch sample cannot find a valid hit, it may be skipped; a dialog can report how many instances were actually placed.

---

## Random Volume + Drop — tips

- Props and environment should have **Colliders** where interaction matters.
- **Simulation** considers **all Rigidbodies** under the spawned hierarchy. If there are none, a temporary rigidbody is added on the **root** only; settings are restored after settle.
- **No colliders at all:** The tool can build **temporary** convex mesh or box colliders from renderers for the simulation, then **remove** them. See [Technical notes](#technical-notes-temporary-colliders--scale) if results look off at extreme scales.

---

## Gizmos

Enable **Show Range Gizmos** to visualize: **Ground Raycast** — green region, orange jitter bounds, light-blue brush preview when the Scene tool is active; **Random Volume + Drop** — magenta wire box (`2 ×` half extents).

Use **Mode help** at the bottom of the window for a short in-editor summary.

---

## Features (at a glance)

- One **Scatter Sources** list (prefabs + scene objects); random pick per instance.
- **Random scale** (min/max) multiplied on each source’s root **local scale** (proportions preserved).
- **Yaw-only or full XYZ** rotation; optional **align to surface normal** when a normal exists.
- **Position jitter** (re-grounded on surfaces when relevant).
- **Include Only / Exclude** roots and **Layer** mask for raycasts.
- **Undo** grouped per batch or per brush stroke.

---

## Troubleshooting

| Issue | Things to try |
|-------|----------------|
| Brush does nothing | Activate the **Object Scatterer** Scene tool (**OS**), not only the window. Check **Raycast Layers** and filters. |
| Batch places fewer objects than count | Some random samples missed a surface; tighten region or enable snap. A dialog may report skips. |
| Drop looks wrong / clipping | Add real colliders to prefabs; check scale. For temp colliders, see [Technical notes](#technical-notes-temporary-colliders--scale). |
| Overlay fields cramped | Use the main window for full labels; overlay uses short labels (**R** / **S**) with tooltips. |

---

## Technical notes (temporary colliders & scale)

When a hierarchy has **no** colliders, the editor builds temporary **MeshCollider** (convex) or **BoxCollider** fallbacks from mesh/skinned data, runs **Physics.SyncTransforms** and mesh refresh, then removes them after settle. Non-uniform scale on compound setups may need the built-in compensation (rigidbody / instance root / collider transform chain). This is best-effort for editor physics, not a substitute for authored colliders on shipping assets.

---

## Support

Questions, licensing, or feature requests: **easystudiowww@gmail.com**
