using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatHandler : MonoBehaviour
{
    public GameObject _endTurnButton;//Static variables don't show in the inspector, so I have to do this to make it work.
    public static Button endTurnButton;

    private bool gameEnded = false;

    private static List<Character> participants = new List<Character>();
    private List<Character> turnOrder = new List<Character>();

    private static float[,] xReference = { { 0, 1, 2, 3, 4, 5 }, { 0.7f, 1.3f, 2.1f, 2.9f, 3.7f, 4.3f } }; //used to convert a grid position to a world position, first index is the valid player x positions, second index is the valid enemy x positions

    public static float getXPos(int x, int y)//returns the world x position of a grid position
    {
        return xReference[x, y];
    }

    void Start()//basically all of this is a placeholder for testing purposes
    {
        endTurnButton = _endTurnButton.GetComponent<Button>();

        AttackHandler.Start();

        GameObject prefab = Resources.Load<GameObject>("CombatPrefabs/CharacterPlaceholder");
        GameObject playerObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);
        playerObject.transform.parent = transform;

        Player player = playerObject.AddComponent(typeof(Player)) as Player;

        player.New(50, new Vector2Int(0, Random.Range(0, 5)), "One armed knight", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/OneArmedKnight"), new List<string>
        {"Spear Strike", "Bifurcated Strike"});

        participants.Add(player);

        GameObject enemyObject = Instantiate(prefab, new Vector3Int(), Quaternion.identity);
        enemyObject.transform.parent = transform;

        Enemy enemy = enemyObject.AddComponent(typeof(Enemy)) as Enemy;
        enemy.New(40, new Vector2Int(1, Random.Range(0,5)), "Skeleton", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/Skeleton"), new List<string>
        {"Spear Strike", "Bifurcated Strike"});

        participants.Add(enemy);

        StartCoroutine(Combat());//starts the combat encounter
    }

    IEnumerator Combat()
    {
        foreach (Character character in participants)//draw 5 cards at the start of combat.
        {
            character.DrawCard(5);
        }

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
                yield return new WaitForSeconds(1);
            }

            foreach (Character character in turnOrder)
            {
                if (character != null)
                {
                    Debug.Log("----------------------");
                    Debug.Log(character.GetName() + "'s turn.");
                    yield return StartCoroutine(character.Turn());
                    yield return new WaitForSeconds(1);
                }
            }

            yield return new WaitForSeconds(1);
            Debug.Log("Round end.");

            bool foundPlayer = false;
            bool foundEnemy = false;

            foreach (Character character in participants)
            {
                foundPlayer = character.GetGridPos().x == 0 || foundPlayer;
                foundEnemy = character.GetGridPos().x == 1 || foundEnemy;

                if (foundPlayer && foundEnemy)
                {
                    break;
                }
            }

            if (!foundPlayer || !foundEnemy)
            {
                gameEnded = true;
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
            0.1f * Mathf.Pow(xPos - 2.5f, 2) - 0.525f :
            0.145f * Mathf.Pow(xPos - 2.5f, 2) + 0.518f,
            -1);
    }
}

public class AttackHandler : MonoBehaviour//handles the creation and storage of each card
{
    private static Dictionary<string, Attack> attacks = new Dictionary<string, Attack>();//stores the actuall code behind each card

    private static Dictionary<string, Dictionary<string, string>> cardInfo = new Dictionary<string, Dictionary<string, string>>();//stores the information of each card (used for player gui)

    private static Dictionary<string, Dictionary<string, float>> cardSize = new Dictionary<string, Dictionary<string, float>>();//stores the font size of each card's name and description

    private static Dictionary<string, TargetType> targetTypes = new Dictionary<string, TargetType>();//stores the different target types that cards can use

    private static GameObject cardPrefab = Resources.Load<GameObject>("CombatPrefabs/Card");

    public static void Start()
    {
        //targetTypes initialization
        targetTypes["ForwardHit"] = new BasicTarget(new List<int> { 0 });
        targetTypes["DiagonalHit"] = new BasicTarget(new List<int> { -1, 1 });

        //attacks initialization
        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Spear Strike"},
            {"Description", "*The user deals 1d4 + 2 damage." }
        }, new Dictionary<string, float>
        {
            {"Name", 0.15f},
            {"Description",  0.15f}
        }, new BasicAttack(targetTypes["ForwardHit"], new List<Effect> { new DamageDice(4, 2) }));


        MakeCardIndex(new Dictionary<string, string>()
        {
            {"Name", "Bifurcated Strike"},
            {"Description", "*The user deals 1d4 damage along both diagonals."}
        }, new Dictionary<string, float>
        {
            {"Name", 0.12f},
            {"Description",  0.15f}
        }, new BasicAttack(targetTypes["DiagonalHit"], new List<Effect> { new DamageDice(4, 0) }));

    }

    public static void UseAttack(Vector2Int userPos, Character character, string attackName) //allows characters to use cards
    {
        attacks[attackName].Activate(userPos, character);
    }

    public static List<int> GetHitPositions(string attackName)
    {
        return attacks[attackName].GetTargetType().GetHitPositions();
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

        GameObject cardObject = Instantiate(cardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        cardObject.transform.parent = character.transform.parent;

        Card card = cardObject.AddComponent<Card>();
        card.New(attacks[cardName], cardInfo[cardName], cardSize[cardName], character);

        return card;
    }
}

public class Character : MonoBehaviour //the superclass of both enemies and players
{
    protected string characterName;

    protected int health;
    protected int speed;
    protected int speedMod;
    protected int movement;

    protected List<string> validCards = new List<string>();

    protected Vector2Int gridPos;

    public virtual void New(int health, Vector2Int gridPos, string name, Sprite sprite, List<string> validCards)//character constructor
    {
        this.health = health;
        this.gridPos = gridPos;
        this.name = name;
        characterName = name;
        this.validCards = validCards;

        transform.GetComponent<SpriteRenderer>().sprite = sprite;

        Move(gridPos);
    }

    public virtual IEnumerator Turn()//signals that it is this character's turn
    {
        Debug.Log("Turn called on Character superclass, use subclasses instead.");
        yield return 0;
    }

    public virtual void DrawCard(int amount)
    {
        Debug.Log("DrawCard used on Character superclass, use subclass instead.");
    }

    protected void Move(Vector2Int moveTo)//changes the grid position to the argument, and updates the world position using getNewPos()
    {
        gridPos = moveTo;
        transform.position = CombatHandler.getNewPos(moveTo);
    }

    public void RollSpeed()//rolls a random speed between 1 and 20, then adds the speedMod to that number
    {
        speed = Random.Range(1, 20) + speedMod;
        speedMod = 0;
    }

    public int TakeDamage(int damage)//updates the character's health, and destroys them if it is at or below 0
    {
        health -= damage;

        if (health <= 0)
        {
            CombatHandler.RemoveCharacter(this);
            Debug.Log(name + " has perished.");
            Destroy(gameObject);
        }

        return health;
    }

    public virtual void RemoveCard(Card card)
    {
        Debug.LogWarning("RemoveCard called on character superclass, use subclass isntead");
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
}

public class Player : Character
{
    private bool isTurn = false;
    private bool turnEnd = false;
    private bool dragging = false;

    protected List<Card> hand = new List<Card>();

    public override void New(int health, Vector2Int gridPos, string name, Sprite sprite, List<string> validCards)//player constructor
    {
        base.New(health, gridPos, name, sprite, validCards);

        gameObject.GetComponent<SpriteRenderer>().color = new Color32(0, 255, 0, 255);

        CombatHandler.endTurnButton.onClick.AddListener(Click);
    }

    public override IEnumerator Turn()
    {
        turnEnd = false;
        isTurn = true;

        movement = 2;

        foreach (Card card in hand)
        {
            card.gameObject.SetActive(true);
        }

        DrawCard(1);

        yield return new WaitUntil(() => turnEnd);

        foreach (Card card in hand)
        {
            card.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.25f / hand.Count);
        }

        isTurn = false;
    }

    public override void DrawCard(int amount)
    {
        StartCoroutine(DrawCardCoroutine(amount));
    }

    public IEnumerator DrawCardCoroutine(int amount)
    {
        List<Card> flipThese = new List<Card>();

        for (int i = 0; i < amount; i++)
        {
            Card card = AttackHandler.MakeCardObject(validCards[Random.Range(0, validCards.Count)], this);
            hand.Add(card);
            flipThese.Add(card);
        }

        foreach(Card card in hand)
        {
            if (!flipThese.Contains(card))
            {
                card.Organize(hand, hand.IndexOf(card));
            }
        }

        foreach(Card card in flipThese)
        {
            card.gameObject.SetActive(true);

            card.GetComponent<Animator>().Play("Card Flip");
            StartCoroutine(Tween.New(new Vector3(-5, 0, 0), card.transform, 0.2f));

            yield return new WaitForSeconds(0.5f);

            card.Organize(hand, hand.IndexOf(card));
        }
    }

    public override void RemoveCard(Card card)
    {
        hand.Remove(card);

        foreach (Card _card in hand)
        {
            _card.Organize(hand, hand.IndexOf(_card));
        }
    }

    public List<Card> GetHand()
    {
        return hand;
    }

    private void OnMouseDrag()
    {
        if (movement > 0)
        {
            dragging = true;

            Vector3 pos_move = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
            transform.position = new Vector3(pos_move.x, pos_move.y, -2);
        }
    }

    private void OnMouseUp()
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

                transform.position = CombatHandler.getNewPos(newPos);
                movement -= movementCost;

                Debug.Log(name + " used " + movementCost + " movement points. " + movement + " remaining.");

                gridPos = newPos;
            }
            else
            {
                transform.position = CombatHandler.getNewPos(gridPos);
            }
        }
    }

    private void Click()
    {
        if (isTurn && !turnEnd)
        {
            turnEnd = true;
        }
    }
}

public class Enemy : Character
{
    protected List<string> hand = new List<string>();

    public override void New(int health, Vector2Int gridPos, string name, Sprite sprite, List<string> validCards)
    {
        base.New(health, gridPos, name, sprite, validCards);

        gameObject.GetComponent<SpriteRenderer>().color = new Color32(255, 0, 0, 255);
    }

    public override IEnumerator Turn()
    {
        movement = 2;

        bool usedCard = true;

        DrawCard(1);

        while (usedCard == true && hand.Count > 0)
        {
            usedCard = false;

            Character target = CombatHandler.GetCharacter(new Vector2Int(Mathf.Abs(gridPos.x - 1), gridPos.y));

            if (target == null)
            {
                target = CombatHandler.GetLowestHealth(Mathf.Abs(gridPos.x - 1));
            }
            Vector2Int targetPos = target.GetGridPos();

            string randomCard = validCards[Random.Range(0, validCards.Count)];

            List<int> validPos = AttackHandler.GetHitPositions(randomCard);

            int closestPos = validPos[0];
            foreach (int pos in validPos)
            {
                if (Mathf.Abs(gridPos.y - (targetPos.y + pos)) <= Mathf.Abs(gridPos.y - (targetPos.y + closestPos)) && targetPos.y + pos >= 0 && targetPos.y + pos <= 5)
                {
                    closestPos = pos;
                }
            }

            targetPos = new Vector2Int(targetPos.x, targetPos.y + closestPos);
            int movementCost = Mathf.Abs(targetPos.y - gridPos.y);

            if (movementCost > 0 && movement > 0)
            {
                Move(new Vector2Int(gridPos.x, gridPos.y + Mathf.Clamp(targetPos.y - gridPos.y, -movement, movement)));
                movement -= movementCost;
                Debug.Log(name + " used " + movementCost + " movement points. " + movement + " remaining.");

                yield return new WaitForSeconds(1);
            }


            Debug.Log(name + " decided to use " + randomCard);

            if (gridPos.y == targetPos.y)//if the enemy is in range to use the card
            {
                AttackHandler.UseAttack(gridPos, this, randomCard);
                hand.Remove(randomCard);

                usedCard = true;

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
                        Debug.Log(name + " decided to use " + nextCard + " again.");

                        AttackHandler.UseAttack(gridPos, this, nextCard);
                        hand.Remove(nextCard);

                        random = Random.Range(0, 2);

                        usedCard = true;

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
    }

    public override void DrawCard(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            hand.Add(validCards[Random.Range(0, validCards.Count)]);
        }
    }
}

public class TargetType
{
    protected List<int> hitPositions;

    public virtual List<int> GetHitPositions()//where enemies can be relative to a target in order for the attack to land
    {
        Debug.LogWarning("GetHitPositions called on superclass, use subclass instead.");
        return new List<int>();
    }

    public virtual List<Character> GetTargets(Vector2Int userPos)
    {
        Debug.Log("Get targets called on TargetType superclass, use subclass instead.");
        return null;
    }
}

public class BasicTarget : TargetType
{
    public override List<int> GetHitPositions()
    {
        List<int> RealPositions = new List<int>();

        foreach (int pos in hitPositions)
        {
            RealPositions.Add(-pos);
        }

        return RealPositions;
    }

    public override List<Character> GetTargets(Vector2Int userPos)
    {
        List<Character> targets = new List<Character>();

        foreach (int offset in hitPositions)
        {
            targets.Add(CombatHandler.GetCharacter(new Vector2Int(Mathf.Abs(userPos.x - 1), userPos.y + offset)));
        }

        return targets;
    }

    public BasicTarget(List<int> hitPositions)
    {
        this.hitPositions = hitPositions;
    }
}

public class Effect
{
    public virtual void Activate(Character target, Character user)
    {
        Debug.Log("Activate called on Effect superclass, use subclass instead");
    }
}

public class DamageDice : Effect
{
    private int faces;
    private int modifier;

    public DamageDice(int faces, int modifier)
    {
        this.faces = faces;
        this.modifier = modifier;
    }

    public override void Activate(Character target, Character user)
    {
        if (target != null)
        {
            int damage = Random.Range(1, faces) + modifier;
            int health = target.TakeDamage(damage);

            Debug.Log(user.GetName() + " dealt " + damage + " damage to " + target.GetName() + ". " + target.GetName() + " is now at " + health + " health.");
        }
    }
}

public class Attack
{
    protected TargetType targetType;

    protected List<Effect> effects;

    public TargetType GetTargetType()
    {
        return targetType;
    }

    public Attack(TargetType targetType, List<Effect> effects)
    {
        this.targetType = targetType;
        this.effects = effects;
    }

    public virtual void Activate(Vector2Int userPos, Character user)
    {
        Debug.Log("Activate called on Attack superclass, use subclass isntead.");
    }
}

public class BasicAttack : Attack
{
    public BasicAttack(TargetType targetType, List<Effect> effects) : base(targetType, effects) {}

    public override void Activate(Vector2Int userPos, Character user)
    {
        List<Character> targets = targetType.GetTargets(userPos);

        foreach (Character character in targets)
        {
            foreach (Effect effect in effects)
            {
                    effect.Activate(character, user);
            }
        }
    }
}

public class Card : MonoBehaviour
{
    private Dictionary<string, string> cardInfo = new Dictionary<string, string>();    

    private Attack attack;

    private Button button;

    private Character character;

    private Animator animator;

    private bool debounce = false;

    public bool move = false;

    public Vector3 goHere = new Vector3(-5, -4.3f, 0);

    public void New(Attack attack, Dictionary<string, string> cardInfo, Dictionary<string, float> cardSize, Character character)
    {
        this.attack = attack;
        this.cardInfo = cardInfo;
        this.character = character;

        transform.position = goHere;
        gameObject.name = character.name + " " + cardInfo["Name"];
        animator = transform.GetComponent<Animator>();

        Transform frontCard = transform.Find("FrontCard");

        button = frontCard.GetComponent<Button>();

        button.onClick.AddListener(Activate);

        TextMeshProUGUI nameText = frontCard.Find("NameCanvas").Find("Name").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = frontCard.Find("DescriptionCanvas").Find("Description").GetComponent<TextMeshProUGUI>();

        nameText.text = cardInfo["Name"];
        nameText.fontSize = cardSize["Name"];

        descriptionText.text = cardInfo["Description"];
        descriptionText.fontSize = cardSize["Description"];

        gameObject.SetActive(false);
    }

    private void Activate()
    {
        if (debounce)
        {
            return;
        }

        Debug.Log(character.name + " used " + cardInfo["Name"] + ".");

        debounce = true;

        attack.Activate(character.GetGridPos(), character);

        character.RemoveCard(this);

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public void Organize(List<Card> hand, int index)
    {
        float xPos = 9 * (index + 0.5f) / hand.Count - 2;
        float yPos = -0.05f * Mathf.Pow(xPos - 2.5f, 2) - 4;
        float zPos = 0.1f * Mathf.Pow(xPos - 2.0f, 2) - 10;
        float rotation = -Mathf.Pow(xPos - 2.5f, 3) / 5;

        Vector3 newPos = new Vector3(xPos, yPos, zPos);

        StartCoroutine(Tween.New(newPos, transform, 0.25f));
        StartCoroutine(Tween.New(Quaternion.Euler(0, 0, rotation), transform, 0.25f));
    }

    public void OnMouseEnter()
    {
        animator.SetBool("Selected", true);
        animator.Play("Select");
    }

    public void OnMouseExit()
    {
        animator.SetBool("Selected", false);
        List<Card> hand = ((Player)character).GetHand();

        Organize(hand, hand.IndexOf(this));
    }
}

public class Tween
{
    public static IEnumerator New(Vector3 targetPos, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Vector3 startPos = transform.position;

        while (Time.time - startTime <= tweenTime)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return null;
        }

        transform.position = targetPos;
    }

    public static IEnumerator New(Quaternion targetRot, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Quaternion startRot = transform.rotation;

        while (Time.time - startTime <= tweenTime)
        {
            transform.rotation = Quaternion.Lerp(startRot, targetRot, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return null;
        }

        transform.rotation = targetRot;
    }
}