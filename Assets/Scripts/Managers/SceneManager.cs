using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
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
        thing.transform.parent = null;
        SceneManager.MoveGameObjectToScene(thing, SceneManager.GetSceneByName("Manager"));
        thing.transform.parent = MainManager.thePit;
        thing.SetActive(false);
    }

    public void Summon(GameObject thing, string scene)
    {
        thing.transform.parent = null;
        SceneManager.MoveGameObjectToScene(thing, SceneManager.GetSceneByName(scene));
        thing.SetActive(true);
    }

    private IEnumerator Load(string sceneName)
    {
        string current = SceneManager.GetActiveScene().name;

        AsyncOperation loaded = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        yield return new WaitUntil(() => loaded.isDone);

        if (!current.Equals("Manager"))
        {
            AsyncOperation unloaded = SceneManager.UnloadSceneAsync(current);

            yield return new WaitUntil(() => unloaded.isDone);
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

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