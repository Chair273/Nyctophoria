using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CombatStarter : MonoBehaviour
{
    public CapsuleCollider2D mainCollider;

    private bool debounce = false;

    private string type;

    private static List<string> validEnemies = new List<string> { "Skeleton", "CryptKeeper" };

    public string GetCharType()
    {
        return type;
    }

    public void Disable()
    {
        debounce = true;
        mainCollider.enabled = false;
    }

    public IEnumerator DelayedEnable()
    {
        WaitForEndOfFrame wait = new WaitForEndOfFrame();

        Transform player = RoomGenerator.main.player;
        float endTime = Time.time + 2;

        while (player.gameObject != null && Time.time < endTime && ((Vector2)player.position - (Vector2)transform.position).magnitude < 0.4f)
        {
            yield return wait;
        }

        debounce = false;
        mainCollider.enabled = true;
    }

    private void Start()
    {
        type = validEnemies[Random.Range(0, validEnemies.Count)];
        transform.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/" + type);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !debounce && !MainManager.combatManager.combat)
        {
            debounce = true;

            Transform player = RoomGenerator.main.player;
            List<GameObject> temp = MainManager.roomManager.GetContents();
            List<GameObject> contents = new List<GameObject>();

            foreach (GameObject obj in temp)
            {
                if (obj.transform.GetComponent<CombatStarter>() != null)
                {
                    contents.Add(obj);
                }
            }

            contents = contents.OrderByDescending(obj => ((Vector2)obj.transform.position - (Vector2)player.position).magnitude).ToList();
            int endIndex = Mathf.Clamp(contents.Count, 0, 6);

            for (int i = 0; i < endIndex; i++)
            {
                MainManager.characterManager.AddCharacter(contents[i].transform.GetComponent<CombatStarter>().GetCharType(), contents[i]);
            }

            MainManager.roomManager.UnloadInstant();
            MainManager.sceneManager.LoadScene("Combat");
        }
    }
}