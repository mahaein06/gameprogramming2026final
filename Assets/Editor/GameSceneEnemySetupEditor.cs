#if UNITY_EDITOR
using ithappy.Animals_FREE;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GameSceneEnemySetupEditor
{
    private static readonly string[] AnimalNames = { "Deer", "Horse", "Pinguin", "Penguin", "Dog", "Tiger" };

    static GameSceneEnemySetupEditor()
    {
        EditorApplication.delayCall += EnsureGameSceneEnemySetup;
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += EnsureGameSceneEnemySetup;
    }

    [MenuItem("Tools/GameScene/Ensure Editable Enemy Setup")]
    public static void EnsureGameSceneEnemySetup()
    {
        if (Application.isPlaying || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "GameScene") return;
        if (Object.FindAnyObjectByType<MinimulAnimalControl>(FindObjectsInactive.Include) != null) return;

        bool changed = false;
        foreach (string animalName in AnimalNames)
        {
            GameObject root = FindAnimalRoot(animalName);
            if (root == null) continue;

            changed |= EnsureComponent<EnemyStatus>(root) != null;
            changed |= EnsureComponent<EnemyAI>(root) != null;

            EnemyStatus status = root.GetComponent<EnemyStatus>();
            if (status != null)
            {
                int beforeCount = root.transform.childCount;
                status.EnsureEditableHPBar();
                changed |= root.transform.childCount != beforeCount;
            }
        }

        if (changed && !Application.isPlaying && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    private static T EnsureComponent<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        if (component != null) return component;

        component = Undo.AddComponent<T>(root);
        EditorUtility.SetDirty(root);
        return component;
    }

    private static GameObject FindAnimalRoot(string animalName)
    {
        GameObject exact = GameObject.Find(animalName);
        if (exact != null && exact.GetComponent<CreatureMover>() != null)
        {
            return exact;
        }

        foreach (CreatureMover mover in Object.FindObjectsByType<CreatureMover>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mover.name == animalName || mover.name.StartsWith(animalName + " ") || mover.name.StartsWith(animalName + "_"))
            {
                return mover.gameObject;
            }
        }

        return null;
    }
}
#endif




