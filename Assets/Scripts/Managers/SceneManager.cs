using System.Collections;
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
        StartCoroutine(setScene(sceneName));
    }

    public void LoadScene(string sceneName, string unload)
    {
        StartCoroutine(switchScene(sceneName, unload));
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

    private IEnumerator setScene(string sceneName)
    {
        AsyncOperation loaded = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        yield return new WaitUntil(() => loaded.isDone);

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

    private IEnumerator switchScene(string sceneName, string unload)
    {
        AsyncOperation loaded = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        yield return new WaitUntil(() => loaded.isDone);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        SceneManager.UnloadSceneAsync(unload);

        if (unload.Equals("Overworld"))
        {
            MainManager.roomManager.UnloadCurrent();
        }

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
