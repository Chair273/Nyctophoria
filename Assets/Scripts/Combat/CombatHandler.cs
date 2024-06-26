using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

using Random = UnityEngine.Random;
using UnityEngine.Rendering.Universal;

//----------Game----------\\

public class CombatHandler : MonoBehaviour
{
    public static CombatHandler main;

    public Button endTurnButton;

    public GameObject preTurnGui;
    public GameObject exhaustionDC;
    public GameObject drawPile;
    public GameObject itemGui;

    public TextMeshProUGUI movementGui;

    private Character currentCharacter;

    private List<Character> participants = new List<Character>();

    private GameObject charPrefab;

    private bool gameEnded = false;

    private int totalPlayerHealth;

    private VolumeProfile volumeProfile;

    private List<Character> turnOrder = new List<Character>();

    private float[,] xReference = { { -3.35f, -2.05f, -0.65f, 0.65f, 2.05f, 3.35f }, { -2.35f, -1.45f, -0.45f, 0.45f, 1.45f, 2.35f } }; //used to convert a grid position to a world position, first index is the valid player x positions, second index is the valid enemy x positions

    public void Begin()
    {

        Transform Gui;
        {
            Gui = transform.Find("Gui");

            Gui.GetComponent<Canvas>().worldCamera = MainManager.mainCamera;

            Gui.Find("Background").Find("Fog").gameObject.SetActive(!MainManager.LowGraphicsMode);
            Gui.Find("Background").Find("Void").Find("Fog").gameObject.SetActive(!MainManager.LowGraphicsMode);

            movementGui = Gui.Find("Movement").Find("Movement").GetComponent<TextMeshProUGUI>();
            volumeProfile = Gui.Find("PostProcessing").GetComponent<Volume>().profile;
            endTurnButton = Gui.Find("EndTurnButtonCanvas").Find("EndTurnButton").GetComponent<Button>();
            preTurnGui = Gui.Find("PreTurnGui").gameObject;
            exhaustionDC = Gui.Find("Exhaustion").Find("ExhaustionDC").gameObject;
            drawPile = Gui.Find("DrawPile").gameObject;
            itemGui = Gui.Find("Items").Find("Main").gameObject;

        }//gui declarations

        {
            charPrefab = Resources.Load<GameObject>("CombatPrefabs/CharacterPlaceholder");
            participants = new List<Character>();
            totalPlayerHealth = 0;
            gameEnded = false;
        }//misc declarations

        {
            List<Dictionary<string, object>> charStats = MainManager.characterManager.GetCharacters();
            bool[,] takenPositions = new bool[2, 6];

            for (int i = 0; i < charStats.Count; i++)
            {
                bool isPlayer = (bool)charStats[i]["IsPlayer"];

                GameObject charObject = Instantiate(charPrefab, Gui);
                Type type = isPlayer ? typeof(Player) : typeof(Enemy);
                Character character = charObject.AddComponent(type) as Character;

                Vector2Int pos = new Vector2Int(isPlayer ? 0 : 1, 0);

                for (int v = 0; v < 6; v++)
                {
                    pos = new Vector2Int(pos.x, Random.Range(v, 5));

                    if (!takenPositions[pos.x, pos.y])
                    {
                        takenPositions[pos.x, pos.y] = true;
                        break;
                    }
                }

                Dictionary<string, int> cards = new Dictionary<string, int>();

                foreach (string key in ((Dictionary<string, int>)charStats[i]["Cards"]).Keys)
                {
                    cards.Add(key, ((Dictionary<string, int>)charStats[i]["Cards"])[key]);
                }

                character.New(
                    (int)charStats[i]["Health"],
                    pos,
                    (string)charStats[i]["Name"],
                    (Sprite)charStats[i]["CombatSprite"],
                    cards,
                    (Dictionary<string, int>)charStats[i]["Items"]);

                participants.Add(character);
                charStats[i]["CombatReference"] = charObject;

                if (isPlayer)
                {
                    totalPlayerHealth += (int)charStats[i]["MaxHealth"];
                }

            }
        }//character position setter

        UpdateHealthVignette();
        StartCoroutine(Combat());
    }

    public float getXPos(int x, int y)//returns the world x position of a grid position
    {
        return xReference[x, y];
    }

    public List<Character> GetParticipants()
    {
        return participants;
    }

    public List<Character> GetParticipants(int x)
    {
        List<Character> returnThese = new List<Character>();

        foreach (Character character in participants)
        {
            if (character.GetGridPos().x == x)
            {
                returnThese.Add(character);
            }
        }

        return returnThese;
    }

    public Character GetCharacter(Vector2Int gridPos)
    {
        if (gridPos.y < 0 || gridPos.y > 5)
        {
            return null;
        }

        foreach (Character character in participants)
        {
            if (character.GetGridPos().Equals(gridPos))
            {
                return character;
            }
        }

        return null;
    }

    public Character GetCurrentCharacter()
    {
        return currentCharacter;
    }

    public Character GetLowestHealth(int targetRow)//2 means any row
    {
        int lowestHealth = int.MaxValue;
        Character lowestHealthCharacter = null;

        foreach (Character character in participants)
        {
            int health = character.GetHealth();

            if (health < lowestHealth && (targetRow == 2 || character.GetGridPos().x == targetRow) )
            {
                lowestHealth = health;
                lowestHealthCharacter = character;
            }
        }

        return lowestHealthCharacter;
    }

    public void RemoveCharacter(Character character)
    {
        participants.Remove(character);
    }

    public Vector2Int GetClosestGridPos(Vector3 pos, int xIndex)
    {
        float closestDistance = float.MaxValue;
        Vector2Int closestPos = new Vector2Int(xIndex, 0);

        for (int i = 0; i < 6; i ++)
        {
            if (Vector3.Distance(pos, getNewPos(new Vector2Int(xIndex, i))) < closestDistance)
            {
                closestDistance = Vector3.Distance(pos, getNewPos(new Vector2Int(xIndex, i)));
                closestPos = new Vector2Int(xIndex, i);
            }
        }

        return closestPos;
    }

    public Vector3 getNewPos(Vector2Int moveTo)//gets the physical world space the character should be in corresponding to each grid position
    {
        float xPos = getXPos(moveTo.x, moveTo.y);

        return new Vector3(xPos, moveTo.x == 0 ? 
            0.095f * Mathf.Pow(xPos, 2) - 0.25f : //player equation
            0.085f * Mathf.Pow(xPos, 2) + 1.4f, //enemy equation
            moveTo.x == 0 ? -3: -1);
    }

    public void UpdateHealthVignette()
    {
        float currentHealth = 0f;

        foreach (Character character in participants)
        {
            if (character.GetGridPos().x == 0)
            {
                currentHealth += character.GetHealth();
            }
        }

        Vignette vignette;

        if (!volumeProfile.TryGet(out vignette)) throw new System.NullReferenceException(nameof(vignette));

        vignette.intensity.Override(0.6f - (currentHealth / totalPlayerHealth) / 2);
    }

    private void Start()
    {
        main = this;
    }


    private IEnumerator Combat()
    {
        int round = 1;

        while (!gameEnded)//while there is at least 1 player and 1 enemy alive
        {
            turnOrder = new List<Character>();

            foreach (Character character in participants)
            {
                character.RollSpeed();

                Debug.Log(character.name + " speed: " + character.GetSpeed());

                turnOrder.Add(character);

                yield return new WaitForSeconds(0.5f);
            }

            {
                int index = 1;

                while (index < turnOrder.Count)
                {
                    if (index >= 1 && turnOrder[index].GetSpeed() > turnOrder[index - 1].GetSpeed())
                    {
                        Character temp = turnOrder[index];
                        turnOrder[index] = turnOrder[index - 1];
                        turnOrder[index - 1] = temp;

                        index--;
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            foreach (Character character in turnOrder)
            {
                if (character != null)
                {
                    Debug.Log("----------------------");
                    Debug.Log(character.GetName() + "'s turn.");

                    currentCharacter = character;

                    yield return StartCoroutine(character.Turn(round == 1));
                    yield return new WaitForSeconds(0.5f);

                    bool foundPlayer = false;
                    bool foundEnemy = false;

                    foreach (Character checkCharacter in participants)
                    {
                        foundPlayer = checkCharacter.GetGridPos().x == 0 || foundPlayer;
                        foundEnemy = checkCharacter.GetGridPos().x == 1 || foundEnemy;

                        if (foundPlayer && foundEnemy)
                        {
                            break;
                        }
                    }

                    if (!foundPlayer || !foundEnemy)
                    {
                        gameEnded = true;
                        break;
                    }
                }
            }

            if (!gameEnded)
            {
                yield return new WaitForSeconds(1);
                Debug.Log("Round end.");

                round++;
            }
        }

        Debug.Log("Combat Ended");

        Vignette vignette;

        if (!volumeProfile.TryGet(out vignette)) throw new System.NullReferenceException(nameof(vignette));

        StartCoroutine(Tween.New(1, vignette, 2));
        StartCoroutine(Tween.New(new Color32(0, 0, 0, 255), vignette, 2));

        yield return new WaitForSecondsRealtime(2);

        List<Dictionary<string, object>> charInfo = MainManager.characterManager.GetCharacters();
        bool survivors = false;

        for (int i = charInfo.Count - 1; i >= 0; i--)
        {
            Character character = ((GameObject)charInfo[i]["CombatReference"]).GetComponent<Character>();
            charInfo[i]["Health"] = character.GetHealth();

            if (character.GetGridPos().x == 1)
            {
                if (charInfo[i]["OverworldReference"] != null && character.GetHealth() <= 0)
                {
                    MainManager.roomManager.RemoveObject((GameObject)charInfo[i]["OverworldReference"]);
                }

                charInfo.RemoveAt(i);
            }
            else if (character.GetHealth() > 0)
            {
                survivors = true;
            }
            else
            {
                charInfo.RemoveAt(i);
            }
        }

        MainManager.combatManager.combat = false;

        if (survivors)
        {
            MainManager.sceneManager.LoadScene("Overworld");
        }
        else
        {
            MainManager.GameOver();
        }
    }
}
