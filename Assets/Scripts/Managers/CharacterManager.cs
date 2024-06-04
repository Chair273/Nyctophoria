using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    private static List<Dictionary<string, object>> characters = new List<Dictionary<string, object>>(); //first string key is the player name, second is the name of each stat
    private static Dictionary<string, Dictionary<string, object>> charTemplate;

    public void DefineCharacters()
    {
        charTemplate = new Dictionary<string, Dictionary<string, object>>
    {
        //Players

        {"OneArmedKnight", new Dictionary<string, object>
        {
            {"Name", "One Armed Knight"},
            {"IsPlayer", true},
            {"MaxHealth", 40},
            {"Health",  40},
            {"Cards", new Dictionary<string, int>
            {
                {"Spear Strike", 4 },
                {"Lunge", 3 },
                {"Guard", 2 }
            }},
            {"Items", new Dictionary<string, int>
            {
                {"Placebo", 1 },
                {"Rabbit Bones", 1 }
            } },
            {"CombatSprite", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/OneArmedKnight") },
            {"ObjectReference", null}
        } },

        {"PlagueCaster", new Dictionary<string, object>
        {
            {"Name", "Plague Caster"},
            {"IsPlayer", true },
            {"MaxHealth", 30},
            {"Health",  30},
            {"Cards", new Dictionary<string, int>
            {
                {"Summon Bees", 3 },
                {"Contagion", 2 },
                {"Lesser Ooze", 2 }
            } },
            {"Items", new Dictionary<string, int>
            {
                {"Placebo", 2 }
            } },
            {"CombatSprite", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/PlagueCaster") },
            {"ObjectReference", null}
        } },

        //Enemies

        {"Skeleton", new Dictionary<string, object>
        {
            {"Name", "Skeleton"},
            {"IsPlayer", false },
            {"MaxHealth", 30},
            {"Health",  30},
            {"Cards", new Dictionary<string, int>
            {
                {"Spear Strike", 3 },
                {"Bifurcated Strike", 3 },
                {"Guard", 2 }
            } },
            {"Items", new Dictionary<string, int> ()},
            {"CombatSprite", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/Skeleton") },
            {"ObjectReference", null}
        } },

        {"CryptKeeper", new Dictionary<string, object>
        {
            {"Name", "Crypt Keeper"},
            {"IsPlayer", false },
            {"MaxHealth", 40},
            {"Health",  40},
            {"Cards", new Dictionary<string, int>
            {
                {"Spear Strike", 2 },
                {"Bifurcated Strike", 1 },
                {"Guard", 1 },
                {"Summon Bees", 1 },
                {"Lesser Ooze", 2 }
            } },
            {"Items", new Dictionary<string, int> ()},
            {"CombatSprite", Resources.Load<Sprite>("CombatPrefabs/CharacterSprites/CryptKeeper") },
            {"ObjectReference", null}
        } },

    };
    }

    public void AddCharacter(string charIndex)
    {
        if (!charTemplate.ContainsKey(charIndex))
        {
            Debug.LogError("Character index not contained in template. " + charIndex);
            return;
        }

        Dictionary<string, object> newChar = new Dictionary<string, object>();

        foreach (var kvp in charTemplate[charIndex])
        {
            newChar.Add(kvp.Key, kvp.Value);
        }

        characters.Add(newChar);
    }

    public List<Dictionary<string, object>> GetCharacters()
    {
        return characters;
    }

    public void ClearCharacters()
    {
        characters = new List<Dictionary<string, object>>();
    }
}
