using UnityEngine;
using System.Collections.Generic;

public class CombatStarter : MonoBehaviour
{
    public CapsuleCollider2D mainCollider;

    private bool debounce = false;

    private string type;

    private static List<string> validEnemies = new List<string> { "Skeleton", "CryptKeeper" };

    private void Start()
    {
        type = validEnemies[Random.Range(0, validEnemies.Count)];
        transform.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/" + type);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !debounce && !MainManager.combatManager.combat)
        {
            MainManager.roomManager.RemoveObject(gameObject);

            debounce = true;

            MainManager.roomManager.UnloadInstant();

            MainManager.characterManager.AddCharacter(validEnemies[Random.Range(0, validEnemies.Count)]);

            MainManager.sceneManager.LoadScene("Combat");
        }
    }

}