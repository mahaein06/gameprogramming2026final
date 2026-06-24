using System;
using ithappy.Animals_FREE;
using UnityEngine;
using UnityEngine.AI;

public class MinimulAnimalControl : MonoBehaviour
{
    [SerializeField] private ThirdPersonCamera playerCamera;
    [SerializeField] private bool useCharacterSelectionState = true;
    [SerializeField] private SelectableAnimal selectedAnimal = SelectableAnimal.Dog;
    [SerializeField] private AnimalSlot[] animals = Array.Empty<AnimalSlot>();

    private void Start()
    {
        ApplySelection(GetSelectedAnimal());
    }

    public void ApplySelection(SelectableAnimal animal)
    {
        selectedAnimal = animal;
        if (useCharacterSelectionState)
        {
            CharacterSelectionState.Select(animal);
        }

        AnimalSlot playerSlot = FindSlot(animal);
        Transform playerRoot = playerSlot?.Root;
        ThirdPersonCamera selectedPlayerCamera = ResolvePlayerCamera(playerSlot);
        Camera selectedAimCamera = ResolveAimCamera(selectedPlayerCamera);

        if (selectedPlayerCamera != null && playerRoot != null)
        {
            selectedPlayerCamera.BindPlayer(playerRoot);
            selectedPlayerCamera.ReinitializeFromCurrentTransform();
            selectedPlayerCamera.SetYawOffset(playerSlot?.CameraYawOffset ?? 0f);
        }

        foreach (AnimalSlot slot in animals)
        {
            if (slot == null) continue;

            bool isPlayer = slot.IsAnimal(animal);
            slot.Apply(isPlayer, playerRoot, selectedPlayerCamera, selectedAimCamera);
        }
    }

    private SelectableAnimal GetSelectedAnimal()
    {
        return useCharacterSelectionState ? CharacterSelectionState.SelectedAnimal : selectedAnimal;
    }

    private AnimalSlot FindSlot(SelectableAnimal animal)
    {
        foreach (AnimalSlot slot in animals)
        {
            if (slot != null && slot.IsAnimal(animal))
            {
                return slot;
            }
        }

        return null;
    }

    private ThirdPersonCamera ResolvePlayerCamera(AnimalSlot playerSlot)
    {
        if (playerCamera != null) return playerCamera;

        ThirdPersonCamera selectedCamera = playerSlot?.FindPlayerCamera();
        if (selectedCamera != null) return selectedCamera;

        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.TryGetComponent(out ThirdPersonCamera mainThirdPersonCamera))
        {
            return mainThirdPersonCamera;
        }

        return FindAnyObjectByType<ThirdPersonCamera>();
    }

    private Camera ResolveAimCamera(ThirdPersonCamera selectedPlayerCamera)
    {
        if (selectedPlayerCamera != null && selectedPlayerCamera.TryGetComponent(out Camera playerUnityCamera))
        {
            return playerUnityCamera;
        }

        return Camera.main;
    }

    [Serializable]
    private class AnimalSlot
    {
        [SerializeField] private SelectableAnimal animal;
        [SerializeField] private GameObject root;
        [SerializeField] private CameraArmAim armAim;
        [SerializeField] private CreatureMover mover;
        [SerializeField] private MovePlayerInput input;
        [SerializeField] private MinimulMuzzleShooter shooter;
        [SerializeField] private MinimulNavMeshAnimalAI ai;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField, Range(-180f, 180f)] private float cameraYawOffset;

        public Transform Root => root != null ? root.transform : null;
        public float CameraYawOffset => cameraYawOffset;

        public bool IsAnimal(SelectableAnimal selected)
        {
            return animal == selected;
        }

        public ThirdPersonCamera FindPlayerCamera()
        {
            return root != null ? root.GetComponentInChildren<ThirdPersonCamera>(true) : null;
        }

        public void Apply(bool isPlayer, Transform playerRoot, ThirdPersonCamera playerCamera, Camera aimCamera)
        {
            if (ai != null && isPlayer)
            {
                ai.enabled = false;
            }

            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = !isPlayer;
            }

            if (mover != null)
            {
                mover.enabled = true;
            }

            if (input != null)
            {
                input.enabled = isPlayer;
                if (isPlayer)
                {
                    input.BindMover(mover);
                    input.BindCamera(playerCamera);
                }
            }

            if (armAim != null)
            {
                armAim.enabled = true;
                if (isPlayer)
                {
                    armAim.BindCamera(aimCamera);
                }
                else
                {
                    armAim.BindTarget(playerRoot);
                }
            }

            if (shooter != null)
            {
                shooter.SetInputEnabled(isPlayer);
                if (isPlayer)
                {
                    shooter.BindPlayerStatus(root != null ? root.GetComponent<PlayerStatus>() : null);
                }
            }

            if (ai != null && !isPlayer)
            {
                ai.BindTarget(playerRoot);
                ai.enabled = true;
            }

            EnemyAI enemyAI = root != null ? root.GetComponent<EnemyAI>() : null;
            if (!isPlayer && enemyAI != null && playerRoot != null)
            {
                enemyAI.BindPlayer(playerRoot);
            }
        }
    }
}
