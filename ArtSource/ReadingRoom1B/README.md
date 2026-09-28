# 1B source record

Image generation used the built-in imagegen tool. Original room references were `Docs/ArtReview/PuzzlePlacementV1/1B-front.png` and `1B-side.png`. The generated front view served as the common visual reference for the remaining room and prop images. `image-prompts.json` records the twelve initial follow-up job descriptions; it is not a complete API transcript.

Additional images: gothic reading chair, library ladder, bookbinder block-print portrait, and four-quadrant paper material atlas. All generated originals were retained in Codex's generated_images directory, and working copies are in the project.

Unity source is in `Assets/_BookTower/Editor/ReadingRoom*.cs`; runtime logic is in `Assets/_BookTower/Scripts/Puzzle/ReadingRoom*.cs`. The builder creates real meshes, materials, colliders and ten prop prefabs, and preserves P3's existing scene identity for progress events.

Rebuild with Unity menu `Book Tower/1B 찢어진 열람실 제작`, in Edit mode with `Assets/Scenes/InGame.unity` open. Batch entry `BookTower.EditorTools.ReadingRoomBuilder.RunVerifiedBatch` with `-readingPass 4` rebuilds, captures six GPU views, checks structure and runs the play verifier. Build-only entry is `RunBatch`. Batch Play-only entry is `BookTower.EditorTools.ReadingRoomVerifier.RunBatch`.

Scene backups live in `Logs/ReadingRoomBackups`. Mesh versions for earlier image comparisons remain under the generated asset folder; do not confuse those with the final Pass4 meshes used by the current scene. Individual prop prefabs are art modules; room/puzzle links are connected by the builder.

No edits to the Unity camera/player movement specification or F0/1A puzzle rules were part of this room build. Shared F and mouse interaction dispatch gained the ReadingRoomControl route.
