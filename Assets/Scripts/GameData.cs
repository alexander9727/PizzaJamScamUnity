// Ignore Spelling: Mana

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class GameData
{
    static GameData instance;
    public static bool IsReady => instance != null;

    private GameData()
    {
    }

    public static GameData CreateOrGet()
    {
        if (instance == null)
        {
            return new GameData();
        }
        else
        {
            return instance;
        }
    }

    HashSet<string> collectedItems;

    public event Action<float, float> onManaChanged;
    private int mana;
    public int lettersPerSecond;
    Dictionary<string, int> intValues;
    Dictionary<string, string> stringValues;
    Dictionary<string, DialogueData> dialogues;
    Dictionary<string, WizardData> wizards;
    Dictionary<string, UpgradeData> upgradeData;

    Sprite[] allSprites;
    AudioClip[] allSounds;
    internal void Init(string data, Sprite[] sprites, AudioClip[] sounds)
    {
        allSprites = sprites;
        allSounds = sounds;
        intValues = new Dictionary<string, int>();
        stringValues = new Dictionary<string, string>();
        collectedItems = new HashSet<string>();

        JObject jData = JObject.Parse(data);
        InitializeDialogues(jData["DialogueList"]);
        InitializeWizards(jData["Wizards"]);
        SetUpgradeData(jData["Upgrades"]);
        SetDefaultData(jData["DefaultData"]);
    }

    private void SetUpgradeData(JToken jToken)
    {
        upgradeData = new Dictionary<string, UpgradeData>();
        foreach (JToken token in jToken)
        {
            var obj = token.ToObject<JProperty>();
            upgradeData.Add(obj.Name, new UpgradeData(obj.Value, this));
        }

        //foreach(var kvp in upgradeData)
        //{
        //    //Debug.Log($"Upgrade {kvp.Key} costs {kvp.Value.upgradeCost}");
        //}
    }

    private void SetDefaultData(JToken jToken)
    {
        if (int.TryParse(jToken["Start"]["LettersPerSecond"].Value<string>(), out int letters))
        {
            lettersPerSecond = letters;
        }
        else
        {
            lettersPerSecond = 30;
        }
        if (int.TryParse(jToken["Start"]["StartingMana"].Value<string>(), out int startMana))
        {
            mana = startMana;
        }
        else
        {
            mana = 0;
        }
        onManaChanged?.Invoke(mana, 0);
    }

    private void InitializeDialogues(JToken jToken)
    {
        dialogues = new Dictionary<string, DialogueData>();
        foreach (JToken token in jToken)
        {
            var obj = token.ToObject<JProperty>();
            dialogues.Add(obj.Name, new DialogueData(obj.Value, this));
        }
    }

    private void InitializeWizards(JToken jToken)
    {
        wizards = new Dictionary<string, WizardData>();
        foreach (JToken token in jToken)
        {
            var obj = token.ToObject<JProperty>();
            wizards.Add(obj.Name, new WizardData(obj.Value, this));
        }
    }

    #region Mana
    public bool UseMana(int manaChange)
    {
        if (mana < manaChange) return false;

        mana -= manaChange;
        onManaChanged?.Invoke(mana, -manaChange);
        return true;
    }
    public void GainMana(int manaChange)
    {
        mana += manaChange;
        onManaChanged?.Invoke(mana, manaChange);
    }
    #endregion

    #region IntValue
    public int GetInt(string key)
    {
        key = key.Trim();
        if (intValues.TryGetValue(key, out int value)) return value;
        return 0;
    }
    public void SetInt(string key, int value)
    {
        key = key.Trim();

        if (intValues.ContainsKey(key))
        {
            intValues[key] = value;
        }
        else
        {
            intValues.Add(key, value);
        }
    }
    public void Increment(string key)
    {
        key = key.Trim();

        if (intValues.ContainsKey(key))
        {
            intValues[key]++;
        }
        else
        {
            intValues.Add(key, 1);
        }
    }
    #endregion

    #region StringValue
    public string GetString(string key)
    {
        key = key.Trim();
        if (stringValues.TryGetValue(key, out string value)) return value;
        return string.Empty;
    }
    public void SetString(string key, string value)
    {
        key = key.Trim();
        value = value.Trim();
        if (stringValues.ContainsKey(key))
        {
            stringValues[key] = value;
        }
        else
        {
            stringValues.Add(key, value);
        }
    }
    #endregion

    #region Inventory
    public void AddItem(string key)
    {
        collectedItems.Add(key);
    }
    public void RemoveItem(string key)
    {
        collectedItems.Remove(key);
    }
    public bool HasObject(string key)
    {
        Debug.Log($"Checking for {key}");
        return collectedItems.Contains(key);
    }
    #endregion

    #region Callback Creation
    internal string ValueGetter(string methodName, string p)
    {
        methodName = methodName.Trim();
        p = p.Trim();
        switch (methodName)
        {
            case "GetDialogueFromCharacter":
                DialogueData d = GetDialogueFromCharacter(p);
                if (d != null)
                {
                    return d.DialogueText;
                }
                return string.Empty;
        }

        return string.Empty;
    }

    internal Func<bool> CreateCondition(string methodName, string p)
    {
        methodName = methodName.Trim();
        p = p.Trim();
        switch (methodName)
        {
            case "HasObject":
                return () =>
                {
                    return HasObject(p);
                };
        }

        return () => true;
    }

    internal Func<bool> CreateCondition(string methodName, string parameter, string value)
    {
        methodName = methodName.Trim();
        parameter = parameter.Trim();
        value = value.Trim();
        switch (methodName)
        {
            case "Get":
                if (int.TryParse(value, out int v))
                {
                    return () =>
                    {
                        return GetInt(parameter) == v;
                    };
                }
                else
                {
                    return () =>
                    {
                        return GetString(parameter) == value;
                    };
                }

        }
        return () => true;
    }

    internal Action CreateFunction(string methodName, string p)
    {
        methodName = methodName.Trim();
        p = p.Trim();
        switch (methodName)
        {
            case "AddToInventory":
                return () =>
                {
                    //Debug.Log($"Adding item {p}");
                    AddItem(p);
                };
            case "RemoveFromInventory":
                return () =>
                {
                    RemoveItem(p);
                };
            case "Increment":
                return () =>
                {
                    Increment(p);
                };
            case "StartDialogue":
                return () =>
                {
                    GameManager.instance.StartDialogue(p);
                };
            case "EnableWizard":
                return () =>
                {
                    ToggleWizardState(p, true);
                };
            case "DisableWizard":
                return () =>
                {
                    ToggleWizardState(p, false);
                };
            case "ChangeMana":
                return () =>
                {
                    if (int.TryParse(p, out int v))
                    {
                        GainMana(v);
                    }
                };

        }

        return () => { };
    }

    internal Action CreateFunction(string methodName, string parameter, string value)
    {
        methodName = methodName.Trim();
        parameter = parameter.Trim();
        value = value.Trim();
        switch (methodName)
        {
            case "Set":
                if (int.TryParse(value, out int v))
                {
                    return () =>
                    {
                        SetInt(parameter, v);
                    };
                }
                else
                {
                    return () =>
                    {
                        SetString(parameter, value);
                    };
                }
        }
        return () => { };
    }
    #endregion

    void ToggleWizardState(string wizardName, bool state)
    {
        if (wizards.TryGetValue(wizardName, out var wizard))
        {
            wizard.isVisible = state;
        }
    }

    #region Getters
    internal Sprite GetSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;
        spriteName = spriteName.Trim();
        foreach (var sprite in allSprites)
        {
            if (sprite == null) continue;
            if (spriteName == sprite.name) return sprite;
        }

        return null;
    }

    internal DialogueData GetDialogue(string id)
    {
        id = id.Trim();
        if(string.IsNullOrEmpty(id)) return null;
        if (dialogues.TryGetValue(id, out var dialogue)) return dialogue;
        return null;
    }

    internal DialogueData GetDialogueFromCharacter(string character)
    {
        var foundValues = dialogues.Where(kvp =>
        {
            return kvp.Value.CharacterId == character && kvp.Value.FlowPriority != -1;
        }).OrderByDescending(kvp => kvp.Value.FlowPriority);

        foreach (var kvp in foundValues)
        {
            var dialogue = kvp.Value;
            Debug.Log(kvp.Key);
            if (dialogue == null) continue;
            if (dialogue.IsTrue()) return dialogue;
        }
        return null;
    }

    public IEnumerable<KeyValuePair<string, WizardData>> GetEnabledWizards()
    {
        foreach (var kvp in wizards)
        {
            if (!kvp.Value.isVisible) continue;
            yield return kvp;
        }
    }

    internal WizardData GetWizard(string character)
    {
        if (wizards.TryGetValue(character, out WizardData c))
        {
            return c;
        }
        return null;
    }

    internal string GetDialogueId(DialogueData dialogueData)
    {
        foreach (var kvp in dialogues)
        {
            if (kvp.Value == dialogueData) return kvp.Key;
        }

        return string.Empty;
    }

    internal AudioClip GetClip(string trackName)
    {
        //Debug.Log("Beggining VO Search");
        foreach (var s in allSounds)
        {
            //Debug.Log(s.name);
            if (s.name == trackName) return s;
        }
        return null;
    }
    internal Dictionary<string, UpgradeData> GetUpgradeData()
    {
        return upgradeData;
    }

    #endregion
}
