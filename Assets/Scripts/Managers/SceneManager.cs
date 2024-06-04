using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    private Dictionary<string, Vector3> cameraPos = new Dictionary<string, Vector3>
    {
        {"Title", new Vector3(0, 0, -500)},
        {"Overworld", new Vector3(0, 1.6f, -500)},
        {"Combat", new Vector3(0, 0, -500)},
    };

    private Dictionary<string, float> cameraSize = new Dictionary<string, float>
    {
        {"Title", 5},
        {"Overworld", 3},
        {"Combat", 5},
    };

    public string GetSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(Load(sceneName));
    }

    public void Banish(GameObject thing)
    {
        thing.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(thing, SceneManager.GetSceneByName("Manager"));
        thing.transform.SetParent(MainManager.thePit, true);
    }

    public void Summon(GameObject thing, string scene)
    {
        thing.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(thing, SceneManager.GetSceneByName(scene));
    }

    private IEnumerator Load(string sceneName)
    {
        if (sceneName.Equals("Combat") && MainManager.combatManager.combat)
        {
            yield break;
        }

        string current = SceneManager.GetActiveScene().name;

        AsyncOperation loaded = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        yield return new WaitUntil(() => loaded.isDone);

        if (!current.Equals("Manager"))
        {
            AsyncOperation unloaded = SceneManager.UnloadSceneAsync(current);

            yield return new WaitUntil(() => unloaded.isDone);
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        MainManager.mainCamera.transform.position = cameraPos[sceneName];
        MainManager.mainCamera.orthographicSize = cameraSize[sceneName];

        if (sceneName.Equals("Overworld"))
        {
            MainManager.roomManager.LoadCurrent();
        }
        else if (sceneName.Equals("Combat"))
        {
            MainManager.combatManager.Begin();
        }
    }
}