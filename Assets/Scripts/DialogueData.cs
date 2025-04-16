using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class DialogueData
{
    public readonly string CharacterId;
    public readonly int FlowPriority;
    public readonly string[] NextDialogues;
    public readonly string DialogueText;
    public readonly Func<bool>[] Conditions;
    public readonly Action[] Functions;
    public readonly string VO;
    //Conditions
    //Functions

    public DialogueData(JToken data, GameData gameData)
    {
        CharacterId = data["Character"].Value<string>();
        VO = data["VO"].Value<string>();
        if (int.TryParse(data["FlowPriority"].Value<string>(), out int result))
        {
            FlowPriority = result;
        }
        else
        {
            FlowPriority = -1;
        }
        NextDialogues = data["NextDialogue"].Value<string>().Replace(" ", "").Split(',');
        DialogueText = data["Dialogue"].Value<string>();
        //Debug.Log($"Text is {DialogueText} and VO is {VO}");

        string conditions = data["Condition"].Value<string>().Replace(" ", "");

        if (string.IsNullOrEmpty(conditions))
        {

        }
        else
        {
            List<Func<bool>> conditionsList = new List<Func<bool>>();
            string[] conditionSplit = conditions.Split(',');

            foreach (string condition in conditionSplit)
            {
                string[] split = condition.Split(":");
                string methodName = split[0];
                string[] param = split[1].Split('|');
                foreach (string p in param)
                {
                    if (p.Contains('='))
                    {
                        string[] valueSplit = p.Split('=');
                        conditionsList.Add(gameData.CreateCondition(methodName, valueSplit[0], valueSplit[1]));
                    }
                    else
                    {
                        conditionsList.Add(gameData.CreateCondition(methodName, p));
                    }

                }
            }

            Conditions = conditionsList.ToArray();
        }

        Functions = Extensions.ConvertCSVToFunctions(data["Function"].Value<string>(), gameData);
    }

    public bool IsTrue()
    {
        if (Conditions == null) return true;
        if (Conditions.Length == 0) return true;
        foreach (var f in Conditions)
        {
            if (!f()) return false;
        }

        return true;
    }

    public void ExecuteFunctions()
    {
        foreach (var func in Functions)
        {
            try
            {
                func();
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed a function call because {ex}");
            }
        }
    }
}
