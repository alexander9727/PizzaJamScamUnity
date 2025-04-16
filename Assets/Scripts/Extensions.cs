using System;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static Vector2 ConvertCSVToVector2(string csv)
    {
        if (string.IsNullOrEmpty(csv))
        {
            return Vector2.zero;
        }
        else
        {
            string[] split = csv.Split(',');
            float.TryParse(split[0], out float x);
            float.TryParse(split[1], out float y);
            return new Vector2(x, y);
        }
    }

    public static Action[] ConvertCSVToFunctions(string functions, GameData gameData)
    {
        if (string.IsNullOrEmpty(functions))
        {
            return new Action[0];
        }
        else
        {
            functions = functions.Replace(" ", "");
            List<Action> functionList = new List<Action>();
            string[] functionsSplit = functions.Split(',');

            foreach (string function in functionsSplit)
            {
                string[] split = function.Split(":");
                string methodName = split[0];
                string[] param = split[1].Split('|');
                foreach (string p in param)
                {
                    if (p.Contains('='))
                    {
                        string[] valueSplit = p.Split('=');
                        functionList.Add(gameData.CreateFunction(methodName, valueSplit[0], valueSplit[1]));
                    }
                    else
                    {
                        functionList.Add(gameData.CreateFunction(methodName, p));
                    }

                }
            }

            return functionList.ToArray();
        }
    }
}
