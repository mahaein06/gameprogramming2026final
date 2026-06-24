using System;
using ithappy.Animals_FREE;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class MinimulAnimalControl : MonoBehaviour
{
    [SerializeField] private ThirdPersonCamera playerCamera;
    [SerializeField] private Slider playerHpSlider;
    [SerializeField] private TMP_Text playerAmmoText;
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

        int enemyCount = 0;
        foreach (AnimalSlot slot in animals)
        {
            if (slot == null) continue;

            bool isPlayer = slot.IsAnimal(animal);
            if (slot.Apply(isPlayer, playerRoot, selectedPlayerCamera, selectedAimCamera, playerHpSlider, playerAmmoText))
            {
                enemyCount++;
            }
        }

        EnemyKillTracker.InitializeForScene(enemyCount);
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
        [SerializeField] private PlayerStatus playerStatus;
        [SerializeField] private EnemyStatus enemyStatus;
        [SerializeField] private Slider enemyHpSlider;
        [SerializeField] private GameObject enemyHpBarRoot;
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

        public bool Apply(
            bool isPlayer,
            Transform playerRoot,
            ThirdPersonCamera playerCamera,
            Camera aimCamera,
            Slider playerHpSlider,
            TMP_Text playerAmmoText)
        {
            if (root == null) return false;

            ResolveRuntimeReferences();
            SetTagSafely(root, isPlayer ? "Player" : "Enemy");
            DisableOldGameSceneSystems();
            if (root.transform.Find("Dog_001_rig") != null && root.TryGetComponent(out Animator dogAnimator))
            {
                dogAnimator.enabled = false;
            }

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
                    shooter.BindPlayerStatus(playerStatus);
                }
                else
                {
                    shooter.BindPlayerStatus(null);
                }
            }

            if (playerStatus != null)
            {
                playerStatus.enabled = isPlayer;
                if (isPlayer)
                {
                    playerStatus.BindUI(playerHpSlider, playerAmmoText);
                }
            }

            if (enemyStatus != null)
            {
                enemyStatus.BindHPBar(enemyHpSlider, enemyHpBarRoot);
                enemyStatus.enabled = !isPlayer;
                enemyStatus.SetEnemyHPBarVisible(!isPlayer);
                if (!isPlayer)
                {
                    enemyStatus.ResetEnemy();
                }
            }

            if (ai != null && !isPlayer)
            {
                ai.BindTarget(playerRoot);
                ai.enabled = true;
            }

            EnemyAI enemyAI = root.GetComponent<EnemyAI>();
            if (!isPlayer && enemyAI != null && playerRoot != null)
            {
                enemyAI.BindPlayer(playerRoot);
            }

            return !isPlayer && enemyStatus != null;
        }

        private void ResolveRuntimeReferences()
        {
            if (root == null) return;

            if (armAim == null) armAim = root.GetComponent<CameraArmAim>();
            if (mover == null) mover = root.GetComponent<CreatureMover>();
            if (input == null) input = root.GetComponent<MovePlayerInput>();
            if (shooter == null) shooter = root.GetComponent<MinimulMuzzleShooter>();
            if (ai == null) ai = root.GetComponent<MinimulNavMeshAnimalAI>();
            if (navMeshAgent == null) navMeshAgent = root.GetComponent<NavMeshAgent>();
            if (playerStatus == null) playerStatus = root.GetComponent<PlayerStatus>();
            if (enemyStatus == null) enemyStatus = root.GetComponent<EnemyStatus>();
        }

        private void DisableOldGameSceneSystems()
        {
            if (root == null) return;

            foreach (EnemyAI oldAi in root.GetComponentsInChildren<EnemyAI>(true))
            {
                oldAi.enabled = false;
            }

            foreach (EnemyTerrainRoamer roamer in root.GetComponentsInChildren<EnemyTerrainRoamer>(true))
            {
                roamer.SetActiveRoaming(false);
                roamer.enabled = false;
            }

            foreach (DogRpgShooter oldShooter in root.GetComponentsInChildren<DogRpgShooter>(true))
            {
                oldShooter.enabled = false;
            }
        }

        private static void SetTagSafely(GameObject target, string tagName)
        {
            try
            {
                target.tag = tagName;
            }
            catch (UnityException)
            {
                Debug.LogWarning($"Tag '{tagName}' is missing. Add it in Project Settings > Tags and Layers.", target);
            }
        }
    }
}
