using Newtonsoft.Json.Linq;
using UnityEngine;

public class UpgradeData
{
    public int upgradeCost;
    public Sprite displayPicture;

    public UpgradeData(JToken data, GameData gameData)
    {
        upgradeCost = data["Cost"].Value<int>();
        displayPicture = gameData.GetSprite(data["Image"].Value<string>());
    }
}
