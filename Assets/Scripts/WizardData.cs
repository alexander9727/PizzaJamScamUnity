using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class WizardData
{
    public string wizardName;
    public string wizardNickName;
    public string about;
    public string likes;
    public string dislikes;
    public string recentPurchase;
    public bool isVisibleByDefault;
    public bool isVisible;
    public Sprite profilePic;
    JToken data;

    public WizardData(JToken data, GameData gameData)
    {
        this.data = data;
        UpdateWizard(gameData);
    }

    public void UpdateWizard(GameData gameData)
    {
        wizardName = GetValue(data["Name"].Value<string>(), gameData);
        wizardNickName = GetValue(data["Nickname"].Value<string>(), gameData);
        about = GetValue(data["About"].Value<string>(), gameData);
        likes = GetValue(data["Likes"].Value<string>(), gameData);
        dislikes = GetValue(data["Dislikes"].Value<string>(), gameData);
        recentPurchase = GetValue(data["Recent Purchase"].Value<string>(), gameData);
        isVisible = isVisibleByDefault = data["IsVisibleByDefault"].Value<string>().ToUpper() != "FALSE";
        profilePic = gameData.GetSprite(data["ProfilePicName"].Value<string>());
        //Debug.Log(this);
    }

    public string DisplayFullInfo()
    {
        return $"<b>Name:</b> {wizardName}\n<b>About:</b> {about}\n<b>Likes:</b> {likes}\n<b>Dislikes:</b> {dislikes}\n<b>Recent Purchase:</b> {recentPurchase}";
    }

    string GetValue(string p, GameData gameData)
    {
        if (p.Contains(':'))
        {
            string[] split = p.Split(':');
            return gameData.ValueGetter(split[0], split[1]);
        }
        return p;
    }

    public override string ToString()
    {
        return $"{wizardName}, {about}, {likes}, {dislikes}, {recentPurchase}";
    }
}
