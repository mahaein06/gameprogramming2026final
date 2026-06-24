# Agent Handoff

## Current Goal
- Add NavMesh-driven AI behavior for non-selected MininulScene animals while keeping selected animal control manual.

## Decisions
- Added `MinimulNavMeshAnimalAI` as a visible scene component. It does not add runtime components.
- `NavMeshAgent` is used for path calculation only; actual movement still goes through `CreatureMover.SetInput(...)` so animal movement animation remains in the existing flow.
- Non-selected animals switch to AI mode through `MinimulAnimalControl`; selected animal keeps `MovePlayerInput`, `CreatureMover`, camera aim, and shooter input.
- Non-selected animals receive the selected player root as both `CameraArmAim` target and `MinimulNavMeshAnimalAI` target.
- AI attack uses `MinimulMuzzleShooter.FireEnemy(damage)`. `EnemyAI` and `PlayerShoot` were not changed for this NavMesh step.
- `detectRange` is drawn as a yellow wire sphere in `OnDrawGizmosSelected()`.

## Files Changed
- `Assets/Minimul/scripts/MinimulNavMeshAnimalAI.cs`
- `Assets/Minimul/scripts/MinimulNavMeshAnimalAI.cs.meta`
- `Assets/Minimul/scripts/MinimulAnimalControl.cs`
- `Assets/Minimul/MininulScene.unity`
- `Assembly-CSharp.csproj`

## Validation
- Ran `dotnet build "마해인 게임.sln" --no-restore --verbosity minimal`.
- Result: success, 0 errors, 2 existing warnings.
- Unity Editor scene load, NavMeshSurface bake, and Play Mode behavior checks were intentionally left for the user.

## Next Steps
- In Unity, open `MininulScene` and bake the NavMeshSurface.
- Confirm selected animal is manually controlled.
- Confirm non-selected animals wander, chase within `detectRange`, stop/aim/fire within `attackRange`, and swap correctly when selection changes.
- Select animal roots in Scene View to see each AI `detectRange` gizmo.
