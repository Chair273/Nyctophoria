using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

using Random = UnityEngine.Random;

//----------Game----------\\

public class CombatHandler : MonoBehaviour
{
    public static Button endTurnButton;

    public static GameObject preTurnGui;
    public static GameObject exhaustionDC;
    public static GameObject drawPile;

    private Transform Gui;

    public static TextMeshProUGUI movementGui;

    private bool gameEnded = false;

    private static List<Character> participants = new List<Character>();
    private List<Character> turnOrder = new List<Character>();

    private static float[,] xReference = { { -0.75f, 0.5f, 1.9f, 3.1f, 4.5f, 5.75f }, { 0.25f, 1.1f, 2.1f, 2.9f, 3.9f, 4.75f } }; //used to convert a grid position to a world position, first index is the valid player x positions, second index is the valid enemy x positions

    public static float getXPos(int x, int y)//returns the world x position of a grid position
    {
        return xReference[x, y];
    }

    void Start()//basically all of this is a placeholder for testing purposes
    {
        Gui = transform.Find("Gui");

        endTurnButton = Gui.Find("EndTurnButtonCanvas").Find("EndTurnButton").GetComponent<Button>();
        preTurnGui = Gui.Find("PreTurnGui").gameObject;
        exhaustionDC = Gui.Find("Exhaustion").Find("ExhaustionDC").gameObject;
        drawPile = Gui.Find("DrawPile").gameObject;

        movementGui = Gui.Find("Movement").Find("Movement").GetComponent<TextMeshProUGUI>();

        AttackHandler.Start();

        GameObject prefab = Resources.Load<GameObject>("CombatPrefabs/CharacterPlaceholder");

        GameObject OAKObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);//One Armed Knight
        OAKObject.transform.parent = transform;

        Player OAK = OAKObject.AddComponent(typeof(Player)) as Player;

        OAK.New(40, new Vector2Int(0, Random.Range(3, 5)), "One Armed Knight", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/OneArmedKnight"), new Dictionary<string, int>//the name and sprite of the character
        { {"Spear Strike", 4 }, {"Bifurcated Strike", 3 }, {"Guard", 2 } });//the cards they have acess to, and the amount of each.

        participants.Add(OAK);

        GameObject PCObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);//Plague Caster
        PCObject.transform.parent = transform;

        Player PC = PCObject.AddComponent(typeof(Player)) as Player;

        PC.New(30, new Vector2Int(0, Random.Range(0, 3)), "Plague Caster", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/PlagueCaster"), new Dictionary<string, int>
        { {"Summon Bees", 3 }, {"Contagion", 2}, {"Lesser Ooze", 2} });

        participants.Add(PC);

        GameObject skeletonObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);
        skeletonObject.transform.parent = transform;

        Enemy skeleton = skeletonObject.AddComponent(typeof(Enemy)) as Enemy;
        skeleton.New(25, new Vector2Int(1, Random.Range(0, 3)), "Skeleton", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/Skeleton"), new Dictionary<string, int>
        {{"Spear Strike", 3 }, {"Bifurcated Strike", 3 }, {"Guard", 1 } });

        participants.Add(skeleton);

        GameObject CKObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);
        CKObject.transform.parent = transform;

        Enemy cryptKeeper = CKObject.AddComponent(typeof(Enemy)) as Enemy;
        cryptKeeper.New(45, new Vector2Int(1, Random.Range(3, 5)), "Crypt Keeper", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/CryptKeeper"), new Dictionary<string, int>
        {{"Spear Strike", 4 }, {"Bifurcated Strike", 2 }, {"Contagion", 1}, {"Lesser Ooze", 1} });

        participants.Add(cryptKeeper);

        StartCoroutine(Combat());//starts the combat encounter
    }

    IEnumerator Combat()
    {
        int round = 1;

        while (!gameEnded)//while there is at least 1 player and 1 enemy alive
        {
            turnOrder = new List<Character>();

            foreach (Character character in participants)
            {
                character.RollSpeed();

                turnOrder.Add(character);
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
                Debug.Log(character.name + " speed: " + character.GetSpeed());
                yield return new WaitForSeconds(0.5f);
            }

            foreach (Character character in turnOrder)
            {
                if (character != null)
                {
                    Debug.Log("----------------------");
                    Debug.Log(character.GetName() + "'s turn.");

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
    }

    public static Character GetCharacter(Vector2Int gridPos)
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

    public static Character GetLowestHealth(int targetRow)//2 means any row
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

    public static void RemoveCharacter(Character character)
    {
        participants.Remove(character);
    }

    public static Vector2Int GetClosestGridPos(Vector3 pos, int xIndex)
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

    public static Vector3 getNewPos(Vector2Int moveTo)//gets the physical world space the character should be in corresponding to each grid position
    {
        float xPos = getXPos(moveTo.x, moveTo.y);

        return new Vector3(xPos, moveTo.x == 0 ? 
            0.1f * Mathf.Pow(xPos - 2.5f, 2) - 1.25f : //player equation
            0.145f * Mathf.Pow(xPos - 2.5f, 2) + 0.518f, //enemy equation
            moveTo.x == 0 ? -3: -1);
    }
}

public class AttackHandler : MonoBehaviour//handles the creation and storage of each card
{
    public static Dictionary<string, Attack> attacks = new Dictionary<string, Attack>();//stores the actuall code behind each card

    private static Dictionary<string, Dictionary<string, string>> cardInfo = new Dictionary<string, Dictionary<string, string>>();//stores the information of each card (used for player gui)

    private static Dictionary<string, Dictionary<string, float>> cardSize = new Dictionary<string, Dictionary<string, float>>();//stores the font size of each card's name and description

    private static Dictionary<string, TargetType> targetTypes = new Dictionary<string, TargetType>();//stores the different target types that cards can use

    private static GameObject cardPrefab = Resources.Load<GameObject>("CombatPrefabs/Gui/Card");

    public static void Start()
    {
        //targetTypes initialization
        targetTypes["ForwardHit"] = new BasicTarget(new List<int> { 0 }, new List<int> { 1 });
        targetTypes["DiagonalHit"] = new BasicTarget(new List<int> { -1, 1 }, new List<int> { 1 });
        targetTypes["AdjacentHit"] = new BasicTarget(new List<int> { -1, 1 }, new List<int> { 0 });
        targetTypes["SelfEffect"] = new BasicTarget(new List<int> { 0 }, new List<int> { 0 }, new List<int> {-1, 0, 1});
        targetTypes["DiagonalSelfChoice"] = new RangedTarget(new List<int> { -1, 1 }, new List<int> { 0 });
        targetTypes["SmallRangedAttack"] = new RangedTarget(new List<int> {-1, 0, 1}, new List<int> { 1 });

        //attacks initialization
        MakeCardIndex(new Dictionary<string, string>() //the name and description on the card
        {
            {"Name", "Spear Strike"},
            {"Description", "*The user deals 1d6 + 2 damage." }
        }, new Dictionary<string, float> //the font size of the name and description
        {
            {"Name", 0.15f},
            {"Description",  0.15f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>> //the dictionary containing the cards target types, and the effects corrisponding to each
        {
            {targetTypes["ForwardHit"], new List<Effect> { new DamageDice(1, 6, 2), new PokeVFX("Spear") } }//This card targets the character in the opposite column to the user, and deals 1d4 + 2 damage, thus it uses the ForwardHit target type and its only effect is a 1d4 + 2 DamageDice
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Bifurcated Strike"},
            {"Description", "*The user deals 1d4 damage along both diagonals."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.11f},
            {"Description",  0.15f}
        }, new BasicAttack(0.2f, new Dictionary<TargetType, List<Effect>> //optional argument to add a delay between each character effected by the attack.
        {
            {targetTypes["DiagonalHit"], new List<Effect> { new DamageDice(1, 4, 0), new PokeVFX("Spear") } }
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Guard"},
            {"Description", "*The user gains the guarded status for one turn.\n*Reduces damage by half."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.12f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["SelfEffect"], new List<Effect> { new ApplyStatus("Guarded", 1), new SelfApplyAnimVFX("Guard") } },
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Contagion"},
            {"Description", "*Call forth a terrible disease to inflict apon your target.\n*Increases damage based on amount.\n*Contagious."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.1f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["ForwardHit"], new List<Effect> { new ApplyStatus("Contagion", 1) } },
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "ContagionSpread"},
            {"Description", "*The user's contagion becomes too much to bear, infesting those nearby.\n*You aren't supposed to be here."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.15f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["AdjacentHit"], new List<Effect> { new ApplyStatus("Contagion", 2) } },
            {targetTypes["ForwardHit"], new List<Effect> { new ApplyStatus("Contagion", 1) } },
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Summon Bees"},
            {"Description", "*Manifest a wave of bees.\n*Poisons and deals damage.\n*Effect diminishes with distance."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.1f}
        }, new BasicAttack(0.1f, new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["ForwardHit"], new List<Effect> { new DamageDice(1, 4, 0), new ApplyStatus("Poison", 3) }},
            {targetTypes["DiagonalHit"], new List<Effect> {new DamageDice(1, 2, 0), new ApplyStatus("Poison", 2) }}
        }));

        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Lesser Ooze"},
            {"Description", "*Coat your enemy in a debilitating slime.\n*Reduces movement for 3 turns.\n*Short range"}
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.1f}
        }, new BasicAttack(new Dictionary<TargetType, List<Effect>>
        {
            {targetTypes["SmallRangedAttack"], new List<Effect> { new ApplyStatus("Oozed", 2) } },
        }));
    }

    public static List<int> GetStandPositions(string attackName)//remember to add a way to distinguish between cards that help and cards that hurt.
    {
        List<int> hitPositions = new List<int>();

        foreach (TargetType targetType in attacks[attackName].GetTargetTypes())//loop through each target type of the attack
        {
            foreach (int pos in targetType.GetStandPositions())//loop through each position the enemy can be in for the attack to land
            {
                if (!hitPositions.Contains(pos))//if the position has not already been considered, add it to the list.
                {
                    hitPositions.Add(pos);
                }
            }
        }

        return hitPositions;
    }

    private static void MakeCardIndex(Dictionary<string, string> newCardInfo, Dictionary<string, float> newCardSize, Attack attack)
    {
        attacks.Add(newCardInfo["Name"], attack);
        cardInfo.Add(newCardInfo["Name"], newCardInfo);
        cardSize.Add(newCardInfo["Name"], newCardSize);
    }

    public static Card MakeCardObject(string cardName, Character character)
    {
        if (!attacks.ContainsKey(cardName))
        {
            Debug.LogWarning("Invalid card key");
            return null;
        }

        GameObject cardObject = Instantiate(cardPrefab);
        cardObject.transform.parent = character.transform.parent;

        Card card = cardObject.AddComponent<Card>();
        card.New(attacks[cardName], cardInfo[cardName], cardSize[cardName], character);

        return card;
    }
}

//----------Characters----------\\

public class Character : MonoBehaviour //the superclass of both enemies and players
{
    public bool usingCard;

    protected static Dictionary<string, Color32> CharacterColors = new Dictionary<string, Color32>
    {
        //-----Players-----\\
        {"One Armed Knight", new Color32(255, 255, 255, 255)},
        {"Plague Caster", new Color32(170, 50, 0, 255) },
        //-----Enemies-----\\
        {"Skeleton", new Color32(255, 255, 255, 255)},
        {"Crypt Keeper", new Color32(200, 180, 210, 255) }
    };

    protected string characterName;

    protected Dictionary<string, int> drawPile;//when drawing a card, they pull from this list
    protected Dictionary<string, int> discardPile;//when using a card, it moves to this (if it doesnt proc exhaustion)

    protected Dictionary<String, Sprite> statusSymbols;

    protected bool exhaustedLastTurn;

    protected int cardsUsed;//the amount of cards used this turn
    protected int exhaustionChance;//the chance of a card proccing exhaustion, represented as a d20
    protected int health;//self explanatory, if it reaches 0 you die.
    protected int speed;//determines the turn order by rolling a d20, higher speeds go first
    protected int speedMod;//arbitrary number to add on to the speed roll
    protected int movement;//amount of grids the character can move on their turn

    protected Color32 baseColor;//color of the character

    protected SpriteRenderer spriteRenderer;

    protected GameObject effectTemplate;

    protected Transform statusContainer;

    protected List<StatusEffect> statusEffects;//list of status effects applied to this character

    protected Vector2Int gridPos;//the grid position of the character.

    public virtual void New(int health, Vector2Int gridPos, string name, Sprite sprite, Dictionary<string, int> drawPile)//character constructor
    {
        this.health = health;
        this.gridPos = gridPos;
        this.name = name;
        characterName = name;
        this.drawPile = drawPile;

        statusSymbols = new Dictionary<string, Sprite>
        {
            {"Guarded", Sprite.Create(Resources.Load<Texture2D>("StatusEffectSprites/Guarded"), new Rect(0, 0, 49, 49), new Vector2(0.5f, 0.5f)) },
            {"Contagion", Sprite.Create(Resources.Load<Texture2D>("StatusEffectSprites/Contagion"), new Rect(0, 0, 49, 49), new Vector2(0.5f, 0.5f)) },
            {"Poison", Sprite.Create(Resources.Load<Texture2D>("StatusEffectSprites/Poison"), new Rect(0, 0, 49, 49), new Vector2(0.5f, 0.5f)) }
        };

        statusEffects = new List<StatusEffect>();

        statusContainer = transform.Find("StatusContainer");
        effectTemplate = Resources.Load<GameObject>("CombatPrefabs/GUI/StatusEffect");

        discardPile = new Dictionary<string, int>();

        if (CharacterColors.ContainsKey(name))
        {
            baseColor = CharacterColors[name];
        }
        else
        {
            baseColor = new Color32(255, 255, 255, 255);
        }

        spriteRenderer = transform.GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = baseColor;

        transform.position = CombatHandler.getNewPos(gridPos);
        Move(gridPos, false);
    }

    public virtual IEnumerator Turn(bool first)//signals that it is this character's turn
    {
        exhaustedLastTurn = false;
        movement = 2;
        cardsUsed = 0;

        for (int i = 0; i < statusEffects.Count; i++)//loops through and activates/reduces any status effects that have an effect at the begining of a turn
        {
            StatusEffect status = statusEffects[i];

            if (status.triggers.Contains("ModifyMovement"))
            {
                movement = Mathf.Clamp(status.Activate(movement), 0, int.MaxValue);
            }

            if (status.triggers.Contains("ReduceOnTurnStart"))
            {
                if (!status.Reduce())
                {
                    i--;
                    OrganizeStatusEffects();
                }
            }
        }
        yield return new WaitForFixedUpdate();
    }

    public virtual void DrawCard(int amount)
    {
    }

    public void Move(Vector2Int moveTo, bool moveOthers)//changes the grid position to the argument, and updates the world position using getNewPos()
    {
        if (moveOthers)
        {
            Character otherCharacter = CombatHandler.GetCharacter(moveTo);

            if (otherCharacter != null)
            {
                Debug.Log(name + " swapped places with " + otherCharacter.name + ".");
                otherCharacter.Move(gridPos, false);
            }
        }

        StartCoroutine(Tween.New(CombatHandler.getNewPos(moveTo), transform, 0.2f));
        gridPos = moveTo;

        if (gridPos.y >= 3)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            statusContainer.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1);
            statusContainer.localScale = new Vector3(1, 1, 1);
        }
    }

    public void RollSpeed()//rolls a random speed between 1 and 20, then adds the speedMod to that number
    {
        speed = Random.Range(1, 20) + speedMod;
        speedMod = 0;
    }

    public List<int> TakeDamage(int damage)//updates the character's health, and destroys them if it is at or below 0
    {
        foreach (StatusEffect status in statusEffects)
        {
            if (status.triggers.Contains("ModifyTakenDamage"))
            {
                damage = status.Activate(damage);
            }
        }

        health -= damage;

        if (health <= 0)
        {
            CombatHandler.RemoveCharacter(this);
            Debug.Log(name + " has perished.");

            StartCoroutine(Die());
        }
        else
        {
            StartCoroutine(DamageVisuals());
        }

        return new List<int> {health, damage};
    }

    private IEnumerator DamageVisuals()
    {
        spriteRenderer.color = new Color32(255, 0, 0, 255);
        yield return StartCoroutine(Tween.New(baseColor, spriteRenderer, 0.2f));
    }

    public void AddStatus(StatusEffect status)
    {
        string effectName = status.type;
        bool found = false;

        int i = 0;
        while (i < statusEffects.Count)
        {
            if (statusEffects[i].type.Equals(effectName))
            {
                if (!statusEffects[i].Stack(status.duration))
                {
                    OrganizeStatusEffects();
                    return;
                }
               
                found = true;
                break;
            }
            i++;
        }

        if (!found)
        {
            statusEffects.Add(status);

            GameObject effectVisual = Instantiate(effectTemplate, statusContainer, false);

            effectVisual.name = effectName;
            effectVisual.transform.Find("Canvas").Find("Amount").GetComponent<TextMeshProUGUI>().text = status.duration.ToString();
            status.effectVisual = effectVisual;

            if (statusSymbols.ContainsKey(effectName))
            {
                effectVisual.GetComponent<SpriteRenderer>().sprite = statusSymbols[effectName];
            }

            OrganizeStatusEffects();
        }
    }

    public void RemoveStatus(StatusEffect status)
    {
        statusEffects.Remove(status);
        OrganizeStatusEffects();
    }

    private void OrganizeStatusEffects()
    {
        {
            int i = 0;

            while (i < statusContainer.childCount)
            {
                Transform v = statusContainer.GetChild(i);
                string effectName = v.name;
                bool found = false;

                foreach (StatusEffect effect in statusEffects)
                {
                    if (effect.type.Equals(effectName))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Destroy(v.gameObject);
                }

                i++;
            }
        }

        int childCount = statusContainer.childCount;
        for (int i = 0; i < childCount; i++)
        {
            statusContainer.GetChild(i).localPosition = new Vector3(-(childCount - 1) / 4f + (i * 0.5f), 0, 0);
        }
    }

    protected virtual IEnumerator Die()
    {
        Destroy(gameObject);
        yield return new WaitForFixedUpdate();
    }

    public virtual void RemoveCard(Card card)
    {
    }

    public int GetSpeed()//returns the character's speed
    {
        return speed;
    }

    public Vector2Int GetGridPos()//returns the character's grid position
    {
        return gridPos;
    }

    public int GetHealth()//return's the character's health
    {
        return health;
    }

    public string GetName()// returns the character's name
    {
        return name;
    }

    private void OnMouseEnter()
    {
        statusContainer.gameObject.SetActive(true);
    }

    private void OnMouseExit()
    {
        statusContainer.gameObject.SetActive(false);
    }
}

public class Player : Character
{
    private int turnStage = 0; //0: not this characters turn, 1: pre turn, 2: turn
    private int drawAmount = 5;

    private bool turnEnd = false;
    private bool dragging = false;

    private List<Card> hand = new List<Card>();

    private List<Card> discardObjects = new List<Card>(); //not actually objects, it contains the list of half destroyed cards that need to be used to show refilling the draw pile.

    private static Dictionary<string, Sprite> Emblems;

    private void Awake()
    {
        Emblems = new Dictionary<string, Sprite>
        {
            {"One Armed Knight", Sprite.Create(Resources.Load<Texture2D>("CombatPrefabs/CharacterSprites/Emblems/OneArmedKnight"), new Rect(0, 0, 81, 81), new Vector2(0.5f, 0.5f)) },
            {"Plague Caster", Sprite.Create(Resources.Load<Texture2D>("CombatPrefabs/CharacterSprites/Emblems/PlagueCaster"), new Rect(0, 0, 81, 81), new Vector2(0.5f, 0.5f))  }
        };
    }

    public override void New(int health, Vector2Int gridPos, string name, Sprite sprite, Dictionary<string, int> drawPile)//player constructor
    {
        base.New(health, gridPos, name, sprite, drawPile);

        transform.Find("StatusContainer").localPosition = new Vector3(0, -0.3f, -1);

        CombatHandler.endTurnButton.onClick.AddListener(Click);

        CombatHandler.preTurnGui.transform.Find("Draw").GetComponent<Button>().onClick.AddListener(() =>
        {
            if (turnStage == 1)
            {
                drawAmount++;
                turnStage = 2;
            }
        });

        CombatHandler.preTurnGui.transform.Find("Fold").GetComponent<Button>().onClick.AddListener(() =>
        {
            if (turnStage == 1)
            {
                Debug.Log(name + " folded.");

                drawAmount += 2;
                turnStage = 0;
            }
        });
    }

    public override IEnumerator Turn(bool first)
    {
        turnEnd = false;
        turnStage = 1;

        CombatHandler.drawPile.GetComponent<Animator>().Play("Open");
        CombatHandler.drawPile.transform.Find("Deck").Find("Emblem").GetComponent<SpriteRenderer>().sprite = Emblems.ContainsKey(name) ? Emblems[name] : null;

        if (drawPile.Count > 0)
        {
            CombatHandler.drawPile.transform.Find("Card").gameObject.SetActive(true);
        }

        CombatHandler.preTurnGui.SetActive(true);
        CombatHandler.preTurnGui.GetComponent<Animator>().Play("Enable");

        yield return new WaitUntil(() => turnStage != 1);

        CombatHandler.preTurnGui.GetComponent<Animator>().Play("Disable");

        yield return new WaitForSeconds(0.3f);

        CombatHandler.preTurnGui.SetActive(false);

        StartCoroutine(base.Turn(first));//trigger any begining-of-turn status effects

        if (turnStage == 2)//if the player did not forfeit their turn (didnt fold)
        {
            if (!exhaustedLastTurn)
            {
                exhaustionChance = Mathf.Clamp(exhaustionChance - 1, 0, 20);
            }

            yield return new WaitForSeconds(0.75f);

            CombatHandler.exhaustionDC.GetComponent<TextMeshProUGUI>().text = "Exhaustion\nRoll " + exhaustionChance + " or above.";
            CombatHandler.exhaustionDC.SetActive(true);

            CombatHandler.movementGui.text = "Movement remaining: " + movement;
            CombatHandler.movementGui.gameObject.SetActive(true);

            yield return StartCoroutine(DrawCardCoroutine(drawAmount));

            CombatHandler.endTurnButton.gameObject.SetActive(true);
            CombatHandler.endTurnButton.transform.GetComponent<Animator>().Play("Enable");

            yield return new WaitUntil(() => turnEnd);

            CombatHandler.endTurnButton.transform.GetComponent<Animator>().Play("Disable");
        }

        turnStage = 0;
        movement = 0;

        foreach (Card card in hand)
        {
            card.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.25f / hand.Count);
        }

        if (drawPile.Count == 0)
        {
            drawPile = discardPile;
            discardPile = new Dictionary<string, int>();

            int amount = discardObjects.Count;
            for (int i = 0; i < amount; i ++)//Remove every card and make it do the return visuals one by one
            {
                StartCoroutine(discardObjects[0].Return());
                discardObjects.RemoveAt(0);

                yield return new WaitForSeconds(0.2f / amount);
            }
        }

        CombatHandler.exhaustionDC.SetActive(false);
        CombatHandler.endTurnButton.gameObject.SetActive(false);
        CombatHandler.movementGui.gameObject.SetActive(false);

        CombatHandler.drawPile.GetComponent<Animator>().SetBool("Open", false);

        yield return new WaitForSeconds(1);
    }

    protected override IEnumerator Die()
    {
        turnEnd = true;
        turnStage = 0;

        Vector3 newPos = new Vector3(transform.position.x, transform.position.y - 0.125f, transform.position.z);//the only difference is that players go down and enemies go up

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 255), spriteRenderer, 0.25f));
        StartCoroutine(Tween.New(newPos, transform, 0.5f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, Random.Range(-4, 5) * 5), transform, 0.25f));

        yield return new WaitForSeconds(0.15f);

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 0), spriteRenderer, 0.4f));

        yield return new WaitForSeconds(0.6f);

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public override void DrawCard(int amount)
    {
        StartCoroutine(DrawCardCoroutine(amount));
    }

    public IEnumerator DrawCardCoroutine(int amount)
    {
        List<Card> flipThese = new List<Card>();
        List<string> keys = new List<string>(drawPile.Keys);

        for (int i = 0; i < amount && drawPile.Count > 0; i++)//create new random cards and remove them from the draw pile
        {
            string randomCard = keys[Random.Range(0, keys.Count)];
            drawPile[randomCard]--;

            Card card = AttackHandler.MakeCardObject(randomCard, this);

            if (drawPile[randomCard] <= 0)
            {
                drawPile.Remove(randomCard);
                keys.Remove(randomCard);
            }

            hand.Add(card);//prematurely add them to the hand list so other cards will organize properly
            flipThese.Add(card);
        }

        drawAmount = 0;

        foreach (Card card in hand)//loop through all the cards already in the characters hand to instantly organize them.
        {
            if (!flipThese.Contains(card))
            {
                card.Organize(hand, hand.IndexOf(card));
            }
        }

        if (drawPile.Count == 0)
        {
            Debug.Log(name + " ran out of cards in their draw pile.");
            CombatHandler.drawPile.transform.Find("Card").gameObject.SetActive(false);
        }

        foreach (Card card in flipThese)//loop through all the new cards and play the flip animation on them.
        {
            card.gameObject.SetActive(true);

            card.GetComponent<Animator>().Play("Card Flip");
            StartCoroutine(Tween.New(new Vector3(-4.25f, 0, 0), card.transform, 0.2f));

            yield return new WaitForSeconds(0.1f);

            StartCoroutine(Tween.New(Quaternion.Euler(0, 0, 0), card.transform, 0.15f));

            yield return new WaitForSeconds(0.2f);

            card.Organize(hand, hand.IndexOf(card));
        }
    }

    public override void RemoveCard(Card card)//removes the card from the players hand, and adds it to the discard pile
    {
        hand.Remove(card);

        cardsUsed++;

        if (cardsUsed > 1)
        {
            exhaustionChance = Mathf.Clamp(exhaustionChance + 2, 0, 20);

            CombatHandler.exhaustionDC.GetComponent<TextMeshProUGUI>().text = "Exhaustion\nRoll " + exhaustionChance + " or higher.";

            int random = Random.Range(1, 21);

            Debug.Log("Chance: " + exhaustionChance + ". Rolled: " + random + ".");

            if (random == 1 || random < exhaustionChance && random != 20)
            {
                exhaustedLastTurn = true;
                Debug.Log(name + " exhausted their " + card.GetName() + " card.");

                StartCoroutine(card.Burn());

                foreach (Card otherCard in hand)//organizes the rest of the cards
                {
                    otherCard.Organize(hand, hand.IndexOf(otherCard));
                }

                return;
            }
        }

        string cardName = card.GetName();

        if (!discardPile.ContainsKey(cardName))
        {
            discardPile.Add(cardName, 0);
        }

        discardPile[card.GetName()]++;

        foreach (Card otherCard in hand)//organizes the rest of the cards
        {
            otherCard.Organize(hand, hand.IndexOf(otherCard));
        }

        discardObjects.Add(card);

        StartCoroutine(card.Remove());
    }

    public List<Card> GetHand()
    {
        return hand;
    }

    private void OnMouseDrag()//If it the players turn, and they have movement points left, allow them to move by dragging to a new grid
    {
        if (movement > 0 && turnStage == 2)
        {
            dragging = true;

            Vector3 pos_move = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
            transform.position = new Vector3(pos_move.x, pos_move.y, -2);
        }
    }

    private void OnMouseUp()//Extension of OnMouseDrag, finds the nearest grid to where you dragged the character and moves them to the closest grid within range of it
    {
        if (dragging)
        {
            dragging = false;

            Vector2Int requestPos = CombatHandler.GetClosestGridPos(transform.position, gridPos.x);
            Vector2Int newPos = new Vector2Int(gridPos.x, gridPos.y + Mathf.Clamp(requestPos.y - gridPos.y, -movement, movement));

            int movementCost = Mathf.Abs(newPos.y - gridPos.y);

            if (movementCost > 0)
            {
                movementCost = Mathf.Clamp(movementCost, 0, movement);

                movement -= movementCost;

                CombatHandler.movementGui.text = "Movement remaining: " + movement;
                Debug.Log(name + " used " + movementCost + " movement points. " + movement + " remaining.");

                Move(newPos, true);
            }
            else
            {
                transform.position = CombatHandler.getNewPos(gridPos);
            }
        }
    }

    private void Click()//ends the players turn, remember to rename this if more buttons require a dedicated click function.
    {
        if (turnStage == 2 && !turnEnd)
        {
            turnEnd = true;
        }
    }
}

public class Enemy : Character
{
    protected List<string> hand = new List<string>();

    public override void New(int health, Vector2Int gridPos, string name, Sprite sprite, Dictionary<string, int> drawPile)
    {
        base.New(health, gridPos, name, sprite, drawPile);
        transform.Find("StatusContainer").localPosition = new Vector3(0, 1.5f, -1);

        DrawCard(5);
    }

    public override IEnumerator Turn(bool first)
    {
        StartCoroutine(base.Turn(first));

        if (health <= 0)
        {
            yield break;
        }

        if (Random.Range(1, 5) == 1 && hand.Count <= 3 && drawPile.Count >= 2)
        {
            DrawCard(2);

            Debug.Log(name + " folded.");

            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        DrawCard(1);

        bool usedCard = true;

        int bravery = Random.Range(2, 21); //end turn if exaustion is above this

        while (usedCard == true && hand.Count > 0 && !(Random.Range(1, 3) == 1 && exhaustionChance > bravery)) //they end their turn under 1 of 3 conditions, 1: if they werent able to use the last card they selected, 2: if they ran out of cards, 3:random 50% chance after exeeding bravery
        {
            usedCard = false;

            Character target = CombatHandler.GetCharacter(new Vector2Int(Mathf.Abs(gridPos.x - 1), gridPos.y));

            if (target == null)
            {
                target = CombatHandler.GetLowestHealth(Mathf.Abs(gridPos.x - 1));
            }

            if (target == null)
            {
                yield break;
            }

            Vector2Int targetPos = target.GetGridPos();

            string randomCard = hand[Random.Range(0, hand.Count)];

            List<int> validPos = AttackHandler.GetStandPositions(randomCard);

            int closestPos = validPos[0];
            foreach (int pos in validPos)
            {
                if (Mathf.Abs(gridPos.y - (targetPos.y + pos)) <= Mathf.Abs(gridPos.y - (targetPos.y + closestPos)) && targetPos.y + pos >= 0 && targetPos.y + pos <= 5)
                {
                    closestPos = pos;
                }
            }

            targetPos = new Vector2Int(targetPos.x, targetPos.y + closestPos);
            int moveDirection = Mathf.Clamp(targetPos.y - gridPos.y, -movement, movement);

            if (Mathf.Abs(moveDirection) > 0 && movement > 0)
            {
                Move(new Vector2Int(gridPos.x, gridPos.y + moveDirection), true);

                movement -= Mathf.Abs(moveDirection);

                Debug.Log(name + " used " + Mathf.Abs(moveDirection) + " movement points. " + movement + " remaining.");

                yield return new WaitForSeconds(1);
            }

            Debug.Log(name + " decided to use " + randomCard);

            yield return new WaitForSeconds(0.5f);

            if (gridPos.y == targetPos.y)//if the enemy is in range to use the card
            {
                StartCoroutine(AttackHandler.attacks[randomCard].Activate(this));
                hand.Remove(randomCard);

                usedCard = true;
                cardsUsed++;
                exhaustionChance = Mathf.Clamp(exhaustionChance + 2, 0, 20);

                if (Random.Range(1, 21) < exhaustionChance)
                {
                    exhaustedLastTurn = true;
                    Debug.Log(name + " exhausted their " + randomCard + " card.");

                }
                else 
                {
                    if (!discardPile.ContainsKey(randomCard))
                    {
                        discardPile.Add(randomCard, 1);
                    }
                    else
                    {
                        discardPile[randomCard]++;
                    }

                    
                }

                yield return new WaitForSeconds(1);

                int random = Random.Range(0, 2);
                while (random == 1 && hand.Count > 0)//repeating 50% chance to use copies of the card, if any
                {
                    string nextCard = "";

                    foreach (string card in hand)
                    {
                        if (card.Equals(randomCard))
                        {
                            nextCard = card;
                            break;
                        }
                    }

                    if (!nextCard.Equals(""))
                    {
                        Debug.Log(name + " used " + nextCard + " again.");

                        StartCoroutine(AttackHandler.attacks[randomCard].Activate(this));
                        hand.Remove(nextCard);

                        exhaustionChance = Mathf.Clamp(exhaustionChance + 2, 0, 20);

                        if (Random.Range(1, 21) < exhaustionChance)
                        {
                            exhaustedLastTurn = true;
                            Debug.Log(name + " exhausted their " + randomCard + " card.");

                        }
                        else
                        {
                            if (!discardPile.ContainsKey(randomCard))
                            {
                                discardPile.Add(randomCard, 0);
                            }

                            discardPile[randomCard]++;
                        }

                        random = Random.Range(0, 2);

                        yield return new WaitForSeconds(1);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else//enemy could not use a card
            {
                Debug.Log("But was not in range");
            }
        }

        if (hand.Count == 0)
        {
            Debug.Log(name + " ran out of cards in their hand.");
        }

        if (drawPile.Count == 0)
        {
            drawPile = discardPile;

            discardPile = new Dictionary<string, int>();
        }
    }

    protected override IEnumerator Die()
    {
        Vector3 newPos = new Vector3(transform.position.x, transform.position.y + 0.3f, transform.position.z);//the only difference is that players go down and enemies go up

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 255), spriteRenderer, 0.15f));
        StartCoroutine(Tween.New(newPos, transform, 0.5f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, Random.Range(-4, 5) * 5), transform, 0.5f));

        yield return new WaitForSeconds(0.15f);

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 0), spriteRenderer, 0.4f));

        yield return new WaitForSeconds(0.6f);

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public override void DrawCard(int amount)
    {
        List<string> keys = new List<string>(drawPile.Keys);

        for (int i = 0; i < amount && drawPile.Count > 0; i++)
        {
            string randomCard = keys[Random.Range(0, keys.Count)];

            hand.Add(randomCard);
            drawPile[randomCard]--;

            if (drawPile[randomCard] <= 0)
            {
                drawPile.Remove(randomCard);
                keys.Remove(randomCard);
            }
        }

        if (drawPile.Count == 0)
        {
            Debug.Log(name + " ran out of cards in their draw pile.");
        }
    }
}

//----------Target Types----------\\

public class TargetType
{
    protected List<int> hitPositions;
    protected List<int> columnPositions;//if it contains 0 it will target friendly characters, if it contains 1 it will target hostile characters, it can contain both.
    protected List<int> standPositions = new List<int>();

    public List<int> GetStandPositions()//where enemies can be relative to a target in order for the attack to land
    {
        return standPositions;
    }

    public List<Vector2Int> GetHitPositions()
    {
        List<Vector2Int> returnThis = new List<Vector2Int>();

        foreach (int i in columnPositions)
        {
            foreach (int v in hitPositions)
            {
                returnThis.Add(new Vector2Int(i, v));
            }
        }
        return returnThis;
    }

    public virtual IEnumerator GetTargets(Vector2Int userPos, Action<List<Character>> callback)
    {
        return null;
    }
}

public class BasicTarget : TargetType
{
    public override IEnumerator GetTargets(Vector2Int userPos, Action<List<Character>> callback)
    {
        List<Character> targets = new List<Character>();

        foreach (int offset in hitPositions)
        {
            foreach (int column in columnPositions)
            {
                targets.Add(CombatHandler.GetCharacter(new Vector2Int(Mathf.Abs(userPos.x - column), userPos.y + offset)));
            }
        }

        yield return new WaitForFixedUpdate();

        callback(targets);
    }

    public BasicTarget(List<int> hitPositions, List<int> columnPositions)
    {
        this.hitPositions = hitPositions;
        this.columnPositions = columnPositions;

        foreach(int i in hitPositions)
        {
            standPositions.Add(-i);
        }
    }

    public BasicTarget(List<int> hitPositions, List<int> columnPositions, List<int> standPositions)
    {
        this.hitPositions = hitPositions;
        this.columnPositions = columnPositions;
        this.standPositions = standPositions;
    }
}

public class RangedTarget : TargetType
{
    public RangedTarget(List<int> hitPositions, List<int> columnPositions)
    {
        this.hitPositions = hitPositions;
        this.columnPositions = columnPositions;

        foreach (int i in hitPositions)
        {
            standPositions.Add(-i);
        }
    }

    public override IEnumerator GetTargets(Vector2Int userPos, Action<List<Character>> callback)
    {
        if (userPos.x == 0)//if the user is a player
        {
            yield return new WaitUntil(() => !Input.GetMouseButton(0));

            GameObject chainObject = MonoBehaviour.Instantiate(Resources.Load<GameObject>("CombatPrefabs/Gui/Spike"), new Vector3(0, 0, -2), Quaternion.identity);
            Vector3 closestPos = new Vector3(-100, 0, 0);
            Vector2Int closestGrid = new Vector2Int();

            while (!Input.GetMouseButton(0))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = -2;

                float closest = Vector3.Distance(mouseWorldPos, closestPos);

                foreach (int offset in hitPositions)
                {
                    if (userPos.y + offset >= 0 && userPos.y + offset <= 5)
                    {
                        foreach (int column in columnPositions)
                        {
                            Vector3 worldPos = CombatHandler.getNewPos(new Vector2Int((int)MathF.Abs(userPos.x - column), userPos.y + offset));
                            float newDistance = Vector3.Distance(mouseWorldPos, worldPos);

                            if (newDistance < closest)
                            {
                                closestPos = worldPos;
                                closestGrid = new Vector2Int(Mathf.Abs(userPos.x - column), userPos.y + offset);
                            }
                        }
                    }
                }

                chainObject.transform.position = CombatHandler.getNewPos(closestGrid) + new Vector3(0, closestGrid.x == 0 ? -0.5f : 1.7f, 0);
                chainObject.transform.rotation = Quaternion.Euler(0, 0, closestGrid.x == 0 ? 0 : 180);

                yield return new WaitForFixedUpdate();
            }
            UnityEngine.Object.Destroy(chainObject);

            callback(new List<Character> { CombatHandler.GetCharacter(closestGrid) });
        }
        else//if the user is a enemy
        {
            Character target = CombatHandler.GetCharacter(new Vector2Int(0, userPos.y));

            if (target != null)
            {
                callback(new List<Character> { target });
            }
            else
            {
                int lowestHealth = int.MaxValue;

                foreach (int offset in hitPositions)
                {
                    Character newTarget = CombatHandler.GetCharacter(new Vector2Int(0, userPos.y + offset));

                    if (target == null || newTarget != null && newTarget.GetHealth() < lowestHealth)
                    {
                        target = newTarget;
                        lowestHealth = newTarget.GetHealth();
                    }
                }

                callback(new List<Character> { target });
            }
        }
    }
}


//----------Effect Types----------\\

public class Effect
{
    float waitTime;

    public Effect(float waitTime)
    {
        this.waitTime = waitTime;
    }

    public virtual void Activate(Character target, Character user)
    {
    }

    public float GetWaitTime()
    {
        return waitTime;
    }
}

public class ApplyStatus : Effect
{
    public static Dictionary<string, Type> statusEffects = new Dictionary<string, Type>
    {
        {"Guarded", typeof(Guarded)},
        {"Contagion", typeof(Contagion)},
        {"Oozed", typeof(Oozed)},
        {"Poison", typeof(Poison)}
    };

    private string statusIndex;

    private int amount;

    public ApplyStatus(string statusIndex, int amount) : base(0)
    {
        this.statusIndex = statusIndex;
        this.amount = amount;
    }

    public ApplyStatus(string statusIndex, int amount, float waitTime) : base(waitTime)
    {
        this.statusIndex = statusIndex;
        this.amount = amount;
    }

    public override void Activate(Character target, Character user)
    {
        if (target != null && statusEffects.TryGetValue(statusIndex, out Type statusType))
        {
            ConstructorInfo cons = statusType.GetConstructor(new[] { typeof(int), typeof(Character) });

            if (cons != null)
            {
                StatusEffect status = (StatusEffect)cons.Invoke(new object[] { amount, target });

                target.AddStatus(status);

                if (target.Equals(user))
                {
                    Debug.Log(target.name + " applied " + status.ToString() + " to themself.");
                }
                else
                {
                    Debug.Log(user.name + " inflicted " + status.ToString() + " on " + target.name + ".");

                }
            }
        }
    }
}

public class DamageDice : Effect
{
    private int amount;
    private int faces;
    private int modifier;

    public DamageDice(int amount, int faces, int modifier) : base(0)
    {
        this.amount = amount;
        this.faces = faces;
        this.modifier = modifier;
    }

    public DamageDice(int amount, int faces, int modifier, float waitTime) : base(waitTime)
    {
        this.amount = amount;
        this.faces = faces;
        this.modifier = modifier;
    }

    public override void Activate(Character target, Character user)
    {
        if (target != null)
        {
            int damage = modifier;

            for (int i = 0; i < amount; i++)
            {
                damage += Random.Range(1, faces + 1);
            }

            List<int> health = target.TakeDamage(damage);

            Debug.Log(user.GetName() + " dealt " + health[1] + " damage to " + target.GetName() + ". " + target.GetName() + " is now at " + health[0] + " health.");
        }
    }
}

public class PokeVFX : Effect
{
    string spriteID;

    public PokeVFX(string spriteID) : base(0)
    { 
        this.spriteID = spriteID;
    }

    public PokeVFX(string spriteID, float waitTime) : base(waitTime)
    {
        this.spriteID = spriteID;
    }

    public override void Activate(Character target, Character user)
    {
        if (!target || ! user)
        {
            return;
        }

        user.StartCoroutine(Poke(target, user));
    }

    private IEnumerator Poke(Character target, Character user)
    {
        GameObject obj = VisualEffectHandler.MakeObject(spriteID, new Vector3(user.transform.position.x, user.transform.position.y + 0.75f, -2));

        SpriteRenderer spriteRenderer = obj.transform.GetComponent<SpriteRenderer>();

        spriteRenderer.color = new Color32(255, 255, 255, 0);

        Vector3 targetPos = new Vector3(target.transform.position.x, target.transform.position.y + 0.75f, -2);
        obj.transform.right = new Vector3(user.transform.position.x, user.transform.position.y + 0.75f, -2) - targetPos;

        user.StartCoroutine(Tween.New(new Color32(255, 255, 255, 255), spriteRenderer, 0.25f));
        user.StartCoroutine(Tween.New(targetPos, obj.transform, 0.8f));

        yield return new WaitForSeconds(0.4f);

        user.StartCoroutine(Tween.New(new Color32(255, 255, 255, 0), spriteRenderer, 0.4f));

        yield return new WaitForSeconds(0.5f);

        VisualEffectHandler.Destroy(obj);
    }
}

public class Wait : Effect
{
    public Wait(float waitTime) : base(waitTime) { }

    public override void Activate(Character target, Character user)
    {

    }
}

public class SelfApplyAnimVFX : Effect//Realy complex code, this one is.
{
    string spriteID;

    public SelfApplyAnimVFX(string spriteID) : base(0)
    {
        this.spriteID = spriteID;
    }

    public SelfApplyAnimVFX(string spriteID, float waitTime) : base(waitTime)
    {
        this.spriteID = spriteID;
    }

    public override void Activate(Character target, Character user)
    {
        if (!target)
        {
            return;
        }

        VisualEffectHandler.MakeObject(spriteID, user.transform.position + new Vector3(0, 0.75f, -0.5f));
    }
}

public class MoveUser : Effect
{
    public MoveUser() : base(0)
    {

    }
}
//----------Status Effects----------\\

public class StatusEffect
{
    protected Character target;

    public string type;

    public int duration;

    public List<string> triggers;

    public GameObject effectVisual;

    public StatusEffect(string type, Character target, List<string> triggers)
    {
        this.type = type;
        this.target = target;
        this.triggers = triggers;
    }

    protected void UpdateAmount()
    {
        if (effectVisual == null) {return;}
        effectVisual.transform.Find("Canvas").Find("Amount").GetComponent<TextMeshProUGUI>().text = duration.ToString();
    }

    public virtual bool Stack(int amount)
    {
        return true;
    }

    public virtual void Activate()
    {
    }

    public virtual int Activate(int armount)
    {
        return 0;
    }

    public virtual bool Reduce()
    {
        return true;
    }
}

public class Guarded : StatusEffect
{
    public Guarded(int duration, Character target) : base("Guarded", target, new List<string> { "ModifyTakenDamage", "ReduceOnTurnStart" })
    { 
        this.duration = duration;
        UpdateAmount();
    }

    public override bool Reduce()
    {
        duration--;
        UpdateAmount();

        Debug.Log("Guarded decreased, new value: " + duration);

        if (duration <= 0)
        {
            Debug.Log("Guarded wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }

        return true;
    }

    public override bool Stack(int duration)
    {
        this.duration += duration;
        Debug.Log("Stacked Guarded, new value: " + this.duration);
        UpdateAmount();
        return true;
    }

    public override int Activate(int damage)
    {
        return (int)Math.Round(damage / 2.0f + 0.5f);
    }

    public override string ToString()
    {
        return "Guarded";
    }
}

public class Contagion : StatusEffect
{
    public Contagion(int duration, Character target) : base("Contagion", target, new List<string> { "ModifyTakenDamage",  "ReduceOnTurnStart" })
    {
        this.duration = duration;
        UpdateAmount();
    }

    private void Explode()
    {
        Debug.Log(target.name + "'s infection progressed to its final stage.");
        int damage = Random.Range(2, 13);//2d6

        target.StartCoroutine(AttackHandler.attacks["ContagionSpread"].Activate(target));

        target.RemoveStatus(this);
        target.TakeDamage(damage);
    }

    public override bool Reduce()
    {
        duration++;
        UpdateAmount();

        Debug.Log("Increased contagion, new value: " + duration);

        if (duration >= 4)
        {
            Explode();

            return false;
        }

        return true;
    }

    public override bool Stack(int duration)
    {
        this.duration += duration;
        UpdateAmount();
        Debug.Log("Stacked contagion, new value: " + this.duration);

        if (this.duration >= 4)
        {
            Explode();
            return false;
        }

        return true;
    }

    public override int Activate(int damage)
    {
        return (damage + duration);
    }

    public override string ToString()
    {
        return "Contagion";
    }
}

public class Poison : StatusEffect
{
    public Poison(int duration, Character target) : base("Poison", target, new List<string> {"ReduceOnTurnStart"})
    {
        this.duration = duration;
    }

    public override bool Reduce()
    {
        duration--;
        UpdateAmount();

        target.TakeDamage(Random.Range(1, 5));

        Debug.Log("Poison decreased, new duration: " + duration);

        if (duration <= 0)
        {
            Debug.Log("Poison wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }

        return true;
    }

    public override bool Stack(int duration)
    {
        this.duration += duration;
        Debug.Log("Stacked Poison, new value: " + this.duration);
        UpdateAmount();

        return true;
    }

    public override string ToString()
    {
        return "Poison";
    }
}

public class Oozed : StatusEffect
{
    public Oozed(int duration, Character target) : base("Oozed", target, new List<string> { "ReduceOnTurnStart", "ModifyMovement" })
    {
        this.duration = duration;
    }

    public override int Activate(int movement)
    {
        Debug.Log("Reduced movement");
        return Mathf.Clamp(movement - 1, 0, int.MaxValue);
    }

    public override bool Reduce()
    {
        duration--;
        UpdateAmount();
        Debug.Log("Oozed decreased, new duration: " + duration);

        if (duration <= 0)
        {
            Debug.Log("Oozed wore off on " + target.name + ".");
            target.RemoveStatus(this);

            return false;
        }

        return true;
    }

    public override bool Stack(int duration)
    {
        this.duration += duration;
        Debug.Log("Stacked Oozed, new value: " + this.duration);
        UpdateAmount();

        return true;
    }

    public override string ToString()
    {
        return "Oozed";
    }
}
//----------Cards----------\\

public class Attack
{
    protected Dictionary<TargetType, List<Effect>> targetEffects;

    protected float waitTime;

    public List<TargetType> GetTargetTypes()
    {
        List<TargetType> targetTypes = new List<TargetType>();

        foreach (TargetType targetType in targetEffects.Keys)
        {
            targetTypes.Add(targetType);
        }

        return targetTypes;
    }

    public Attack(Dictionary<TargetType, List<Effect>> targetEffects, float waitTime)
    {
        this.targetEffects = targetEffects;
        this.waitTime = waitTime;
    }

    public virtual IEnumerator Activate(Character user)
    {
        return null;
    }
}

public class BasicAttack : Attack//simple relationship between the target and effects, only communication is who is being effected.
{
    public BasicAttack(Dictionary<TargetType, List<Effect>> targetEffects) : base(targetEffects, 0) { }

    public BasicAttack(float waitTime, Dictionary<TargetType, List<Effect>> targetEffects) : base(targetEffects, waitTime) { }

    public override IEnumerator Activate(Character user)
    {
        foreach (TargetType targetType in targetEffects.Keys)
        {
            //After much prayer and sacrafice to the dark sphagetti gods, I have been gifted vile arcane knowledge on coroutine returns. God helps us all.
            List<Character> targets = null;

            yield return user.StartCoroutine(targetType.GetTargets(user.GetGridPos(), callback =>
            {
                targets = callback;
            }));

            foreach (Character character in targets)
            {
                foreach (Effect effect in targetEffects[targetType])
                {
                    effect.Activate(character, user);

                    if (effect.GetWaitTime() > 0)
                    {
                        yield return new WaitForSeconds(effect.GetWaitTime());
                    }
                }

                if (waitTime > 0 && character != null)
                {
                    yield return new WaitForSeconds(waitTime);
                }
            }
        }

        user.usingCard = false;
    }
}

public class Card : MonoBehaviour
{
    private List<GameObject> chainObjects = new List<GameObject>();

    private GameObject spikePrefab;

    private Transform frontCard;
    private Transform backCard;

    private Dictionary<string, string> cardInfo = new Dictionary<string, string>();    

    private Attack attack;

    private Button button;

    private Character character;

    private Animator animator;

    private bool debounce = false;
    private bool visualDebounce = false;

    public bool move = false;

    public string GetName()
    {
        return cardInfo["Name"];
    }

    public void Awake()
    {
        spikePrefab = Resources.Load<GameObject>("CombatPrefabs/Gui/Spike");
    }

    public void New(Attack attack, Dictionary<string, string> cardInfo, Dictionary<string, float> cardSize, Character character)
    {
        this.attack = attack;
        this.cardInfo = cardInfo;
        this.character = character;

        transform.position = new Vector3(-5, -3.8f, 1);
        transform.rotation = Quaternion.Euler(0, 0, -10);

        gameObject.name = character.name + " " + cardInfo["Name"];
        animator = transform.GetComponent<Animator>();
        button = transform.Find("Root").GetComponent<Button>();

        button.onClick.AddListener(() => StartCoroutine(Activate()));

        frontCard = transform.Find("Root").Find("FrontCard");
        backCard = transform.Find("Root").Find("BackCard");

        TextMeshProUGUI nameText = frontCard.Find("NameCanvas").Find("Name").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = frontCard.Find("DescriptionCanvas").Find("Description").GetComponent<TextMeshProUGUI>();

        nameText.text = cardInfo["Name"];
        nameText.fontSize = cardSize["Name"];

        descriptionText.text = cardInfo["Description"];
        descriptionText.fontSize = cardSize["Description"];

        gameObject.SetActive(false);
    }

    public void Organize(List<Card> hand, int index)
    {
        float xPos = 9 * (index + 0.5f) / hand.Count - 2;
        float yPos = -0.05f * Mathf.Pow(xPos - 2.5f, 2) - 4;
        float zPos = 0.15f * Mathf.Pow(xPos - 2.0f, 2) - 15;
        float rotation = -Mathf.Pow(xPos - 2.5f, 3) / 5;

        Vector3 newPos = new Vector3(xPos, yPos, zPos);

        gameObject.SetActive(true);

        StartCoroutine(Tween.New(newPos, transform, 0.25f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, rotation), transform, 0.25f));
    }

    public void OnMouseEnter()
    {
        if (!character.usingCard)
        {
            animator.SetBool("Selected", true);
            animator.Play("Select");

            StartCoroutine(Visualize());
        }
    }

    public void OnMouseExit()
    {
        if (!debounce)
        {
            StartCoroutine(RemoveChain());

            animator.SetBool("Selected", false);
            List<Card> hand = ((Player)character).GetHand();

            Organize(hand, hand.IndexOf(this));
        }
    }

    protected virtual IEnumerator Activate()
    {
        if (debounce || character.usingCard)
        {
            yield break;
        }

        Debug.Log(character.name + " used " + cardInfo["Name"] + ".");

        debounce = true;
        character.usingCard = true;

        StartCoroutine(RemoveChain());
        yield return StartCoroutine(attack.Activate(character));

        character.RemoveCard(this);
    }

    private IEnumerator Visualize()
    {
        if (visualDebounce || debounce)
        {
            yield break;
        }

        visualDebounce = true;

        List<Vector2Int> targetPosList = new List<Vector2Int>();

        foreach (TargetType targetType in attack.GetTargetTypes())
        {
            foreach(Vector2Int targetPos in targetType.GetHitPositions())
            {
                if (!targetPosList.Contains(targetPos))
                {
                    targetPosList.Add(targetPos);
                }
            }
        }

        Vector2Int gridPos = character.GetGridPos();

        for (int t = 0; t < targetPosList.Count; t++)
        {
            Vector2Int targetPos = targetPosList[t];

            if (targetPos.y + gridPos.y >= 0 && targetPos.y + gridPos.y <= 5)
            {
                Vector3 pos = CombatHandler.getNewPos(new Vector2Int(targetPos.x, targetPos.y + gridPos.y));

                GameObject chainObject = Instantiate(spikePrefab, pos, Quaternion.identity);
                SpriteRenderer chainRender = chainObject.transform.GetComponent<SpriteRenderer>();

                chainObjects.Add(chainObject);

                chainObject.transform.position += new Vector3(0, Mathf.Abs(gridPos.x - targetPos.x) == 0 ? -1.5f : 3.2f, 0);
                chainObject.transform.rotation = Quaternion.Euler(0, 0, Mathf.Abs(gridPos.x - targetPos.x) == 1 ? 180 : 0);

                chainRender.color = new Color32(255, 0, 0, 0);

                StartCoroutine(Tween.New(new Color32(255, 255, 255, 255), chainRender, 0.25f));
                StartCoroutine(Tween.New(chainObject.transform.position + new Vector3(0, Mathf.Abs(gridPos.x - targetPos.x) == 0 ? 1 : -1.5f, 0), chainObject.transform, 0.25f));
            }
        }

        yield return new WaitForFixedUpdate();

        visualDebounce = false;
    }

    private IEnumerator RemoveChain()
    {
        foreach(GameObject chainObject in chainObjects)
        {
            Destroy(chainObject, debounce ? 0 : 0.5f);
            StartCoroutine(Tween.New(new Color32(0, 0, 0, 0), chainObject.transform.GetComponent<SpriteRenderer>(), 0.25f));
        }

        chainObjects = new List<GameObject>();

        yield return new WaitForSeconds(0.5f);
    }

    public IEnumerator Burn()
    {
        Transform root = transform.Find("Root");

        Destroy(root.Find("BackCard").gameObject);

        Vector3 newPos = new Vector3(transform.position.x, -6, transform.position.z);

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 255), root.Find("FrontCard").GetComponent<SpriteRenderer>(), 0.25f));
        StartCoroutine(Tween.New(newPos, transform, 1));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, Random.Range(-4, 5) * 5), transform, 1));

        yield return new WaitForSeconds(0.3f);

        Destroy(root.Find("FrontCard").Find("NameCanvas").gameObject);
        Destroy(root.Find("FrontCard").Find("DescriptionCanvas").gameObject);

        StartCoroutine(Tween.New(new Color32(0, 0, 0, 0), root.Find("FrontCard").GetComponent<SpriteRenderer>(), 0.8f));

        yield return new WaitForSeconds(1.2f);

        gameObject.SetActive(false);
        Destroy(gameObject, 0.8f);
    }

    public IEnumerator Remove()
    {
        StartCoroutine(Tween.New(transform.position + new Vector3(0, 2.5f, 0), transform, 0.15f));

        yield return new WaitForSeconds(0.15f);

        StartCoroutine(Tween.New(new Vector3(13.5f, -2.5f, -1), transform, 0.5f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, -30), transform, 0.5f));
        StartCoroutine(Tween.New(new Color32(200, 200, 200, 0), frontCard.GetComponent<SpriteRenderer>(), 0.5f));

        Destroy(backCard.gameObject);
    }

    public IEnumerator Return()
    {
        transform.position = new Vector3(13.5f, -2.5f, -1);
        StartCoroutine(Tween.New(new Vector3(-10, -2.5f, -0.5f), transform, 0.5f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, 30), transform, 0.5f));
        StartCoroutine(Tween.New(new Color32(200, 200, 200, 255), frontCard.GetComponent<SpriteRenderer>(), 0.2f));

        yield return new WaitForSeconds(0.3f);

        StartCoroutine(Tween.New(new Color32(200, 200, 200, 0), frontCard.GetComponent<SpriteRenderer>(), 0.2f));

        yield return new WaitForSeconds(0.3f);

        Destroy(gameObject);
    }
}


//----------Misc----------\\

public class Tween
{
    public static IEnumerator New(Vector3 targetPos, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Vector3 startPos = transform.position;

        while (Time.time - startTime <= tweenTime && transform)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return new WaitForFixedUpdate();
        }

        if (transform)
        {
            transform.position = targetPos;
        }
    }

    public static IEnumerator New(Quaternion targetRot, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Quaternion startRot = transform.rotation;

        while (Time.time - startTime <= tweenTime && transform)
        {
            transform.rotation = Quaternion.Lerp(startRot, targetRot, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return new WaitForFixedUpdate();
        }

        if (transform)
        {
            transform.rotation = targetRot;
        }
    }

    public static IEnumerator New(Color32 targetColor, SpriteRenderer spriteRenderer, float tweenTime)
    {
        float startTime = Time.time;

        Color32 startColor = spriteRenderer.color;

        while (Time.time - startTime <= tweenTime && spriteRenderer)
        {
            spriteRenderer.color = Color32.Lerp(startColor, targetColor, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return new WaitForFixedUpdate();
        }

        if (spriteRenderer)
        {
            spriteRenderer.color = targetColor;
        }
    }
}

public class VisualEffectHandler : MonoBehaviour
{
    private static Dictionary<string, GameObject> VFXSprites = new Dictionary<string, GameObject>
    {
        { "Spear", Resources.Load<GameObject>("CombatPrefabs/VFX/Spear") },
        { "Guard", Resources.Load<GameObject>("CombatPrefabs/VFX/Guard") }
    };

    public static GameObject MakeObject(string spriteId, Vector3 pos)
    {
        return Instantiate(VFXSprites[spriteId], pos, Quaternion.identity);
    }

    public static void Destroy(GameObject obj, float time)
    {
        Destroy(obj);
    }
}