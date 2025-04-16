using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [SerializeField] Sprite[] allSprites;
    [SerializeField] AudioClip[] allSounds;
    [SerializeField] TextMeshProUGUI currentMana;
    [Header("Common screens")]
    [SerializeField] GameObject gameOverScreen;
    [SerializeField] GameObject startScreen;
    [SerializeField] GameObject manaChangePrefab;
    [SerializeField] AudioSource gameWonSource;

    [Header("Dialogue Data")]
    [SerializeField] GameObject conversationScreen;
    [SerializeField] GameObject targetPrefab;
    [SerializeField] GameObject callButton;
    [SerializeField] GameObject homeScreenButton;
    [SerializeField] GameObject dialogueBox;
    [SerializeField] Transform dialogueHolderParent;
    [SerializeField] Transform dialogueOptionsParent;
    [SerializeField] AudioSource vOSource;
    [SerializeField] AudioSource disconnectSource;
    [SerializeField] AudioSource ringSource;
    string selectedCharacter;

    [Header("Wizard Display")]
    [SerializeField] GameObject homeScreen;
    [SerializeField] Transform wizardDisplayParent;

    [Header("Upgrade Screen")]
    [SerializeField] GameObject upgradeScreen;
    [SerializeField] Transform upgradeListParent;
    [SerializeField] AudioSource levelUpSource;

    GameData gameData;

    private void Awake()
    {
        instance = this;
        homeScreen.SetActive(false);
        conversationScreen.SetActive(false);
        upgradeScreen.SetActive(false);
        currentMana.text = string.Empty;
        gameOverScreen.SetActive(false);
        startScreen.SetActive(false);
    }

    const string url = "https://script.google.com/macros/s/AKfycbwiZSp-giY-PBY1Hvm_pAfBwgfk0cMNpBknir1iPFEavRnE0pI5rljIHNC5dNToXghnpg/exec";
    IEnumerator Start()
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        //Parse data
        gameData = GameData.CreateOrGet();
        gameData.onManaChanged += OnManaChanged;
        gameData.Init(request.downloadHandler.text, allSprites, allSounds);
        startScreen.SetActive(true);
        ShowWizardSelection();
    }

    private void OnDestroy()
    {
        if (gameData != null)
        {
            gameData.onManaChanged -= OnManaChanged;
        }
    }

    private void OnManaChanged(float mana, float change)
    {
        currentMana.text = $"{mana}";
        if (change == 0) return;
        ShowPopup($"{change:+#;-#;0}");
    }

    public void ShowPopup(string text)
    {
        GameObject g = Instantiate(manaChangePrefab, startScreen.transform.parent);
        g.GetComponent<TextMeshProUGUI>().text = text;
    }

    public void ShowWizardSelection()
    {
        upgradeScreen.SetActive((false));
        conversationScreen.SetActive(false);
        homeScreen.SetActive(true);
        int index = 0;
        foreach (var wizard in gameData.GetEnabledWizards())
        {
            wizard.Value.UpdateWizard(gameData);
            GameObject wizardPrefab = GetTransformPooler(wizardDisplayParent, index);
            wizardPrefab.gameObject.SetActive(true);
            wizardPrefab.GetComponentInChildren<TextMeshProUGUI>().text = wizard.Value.wizardName;
            wizardPrefab.GetComponentInChildren<Image>().sprite = wizard.Value.profilePic;
            Button button = wizardPrefab.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                StartDialogueOnCharacter(wizard.Key);
            });
            index++;
        }
        gameOverScreen.SetActive(index == 0);
        if (gameOverScreen.activeSelf)
        {
            gameWonSource.Play();
        }
        for (; index < wizardDisplayParent.childCount; index++)
        {
            GetTransformPooler(wizardDisplayParent, index).SetActive(false);
        }
    }

    public void ShowUpgradeScreen()
    {
        upgradeScreen.SetActive(true);
        homeScreen.SetActive(false);

        int index = 0;
        foreach (var kvp in gameData.GetUpgradeData())
        {
            GameObject g = GetTransformPooler(upgradeListParent, index);
            g.SetActive(true);
            UpdateSkillDisplay(g, kvp.Value, kvp.Key);
            index++;
        }

        for (; index < upgradeListParent.childCount; index++)
        {
            GetTransformPooler(upgradeListParent, index).SetActive(false);
        }
    }

    private void UpdateSkillDisplay(GameObject display, UpgradeData data, string key)
    {
        //int upgradeCost = gameData.GetUpgradeCost(skill, level);
        int upgradeCost = gameData.HasObject(key) ? -1 : data.upgradeCost;
        display.transform.GetChild(0).GetComponent<Image>().sprite = data.displayPicture;
        display.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = upgradeCost < 0 ? string.Empty : $"Mana: {upgradeCost}";
        Button button = display.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        if (upgradeCost > 0)
        {
            button.onClick.AddListener(() =>
            {
                if (gameData.UseMana(upgradeCost))
                {
                    gameData.AddItem(key);
                    UpdateSkillDisplay(display, data, key);
                    levelUpSource.Play();
                }
                else
                {
                    ShowPopup("YOU BROKE");
                    //TODO: Flash counter
                }
            });
        }
    }

    internal void StartDialogueOnCharacter(string character)
    {
        homeScreenButton.SetActive(true);
        selectedCharacter = character;
        homeScreen.SetActive(false);
        conversationScreen.SetActive(true);
        dialogueBox.SetActive(false);
        callButton.SetActive(true);
        WizardData wizard = gameData.GetWizard(character);
        targetPrefab.GetComponent<TextMeshProUGUI>().text = wizard.DisplayFullInfo();
    }

    public void BeginCall()
    {
        homeScreenButton.SetActive(false);
        callButton.SetActive(false);
        dialogueBox.SetActive(true);
        StartDialogue(gameData.GetDialogueFromCharacter(selectedCharacter));
    }

    internal void StartDialogue(DialogueData dialogueData)
    {
        StartDialogue(gameData.GetDialogueId(dialogueData));
    }

    internal void StartDialogue(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        //Debug.Log($"Starting with dialogue {id}");
        StartCoroutine(IterateThroughDialogue(id));
    }

    IEnumerator IterateThroughDialogue(string id)
    {
        dialogueOptionsParent.gameObject.SetActive(false);
        for (int i = 1; i < dialogueHolderParent.childCount; i++)
        {
            Destroy(dialogueHolderParent.GetChild(i).gameObject);
        }

        DialogueData currentDialogue = gameData.GetDialogue(id);
        dialogueBox.SetActive(true);
        bool isOption = false;

        ringSource.Play();
        yield return new WaitForSeconds(2f);
        int maxCalls = 1000;
        string currentId = string.Empty;
        while (currentDialogue != null)
        {
            maxCalls--;
            if(maxCalls <= 0)
            {
                Debug.Log($"Endless loop current dialogue {currentId} {currentDialogue.CharacterId}");
                break;
            }
            currentDialogue.ExecuteFunctions();
            List<DialogueData> nextDialogues = new List<DialogueData>();
            foreach (var s in currentDialogue.NextDialogues)
            {
                DialogueData nd = gameData.GetDialogue(s);
                if (nd == null) continue;

                //Debug.Log($"Checking option {nd.DialogueText}");

                if (nd.IsTrue())
                {
                    Debug.Log(s);
                    nextDialogues.Add(nd);
                }
            }

            if (string.IsNullOrEmpty(currentDialogue.DialogueText) || isOption)
            {
                isOption = false;
                if (nextDialogues.Count == 0)
                {
                    currentDialogue = null;
                    currentId = string.Empty;
                    break;
                }
                else
                {
                    currentId = currentDialogue.NextDialogues[0];
                    currentDialogue = nextDialogues[0];
                }
                continue;
            }
            dialogueOptionsParent.gameObject.SetActive(false);

            Debug.Log(("Setting text"));

            if (!string.IsNullOrEmpty(currentDialogue.DialogueText))
            {
                PlayVO(currentDialogue.VO);
                yield return StartCoroutine(AddTextToScroll(currentDialogue.CharacterId, currentDialogue.DialogueText));
            }
            //break;
            dialogueOptionsParent.gameObject.SetActive(nextDialogues.Count > 1);
            bool proceed = false;
            if (nextDialogues.Count == 0)
            {
                isOption = false;
                proceed = true;
                currentDialogue = null;
                currentId = string.Empty;
                break;
            }
            else if (nextDialogues.Count < 2)
            {
                Button dialogueBoxButton = dialogueBox.GetComponent<Button>();
                dialogueBoxButton.onClick.RemoveAllListeners();
                dialogueBoxButton.onClick.AddListener(() =>
                {
                    proceed = true;
                    if (nextDialogues.Count == 1)
                    {
                        currentId = currentDialogue.NextDialogues[0];
                        currentDialogue = nextDialogues[0];
                    }
                    else
                    {
                        currentId = string.Empty;
                        currentDialogue = null;
                    }
                });
            }
            else
            {
                int dialogueIndex = 0;
                for (dialogueIndex = 0; dialogueIndex < nextDialogues.Count; dialogueIndex++)
                {
                    int i = dialogueIndex;
                    DialogueData dialogue = nextDialogues[dialogueIndex];
                    GameObject optionButton = GetTransformPooler(dialogueOptionsParent, dialogueIndex);
                    optionButton.SetActive(true);
                    optionButton.GetComponentInChildren<TextMeshProUGUI>().text = dialogue.DialogueText;
                    Button button = optionButton.GetComponent<Button>();
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        currentId = currentDialogue.NextDialogues[i];
                        isOption = true;
                        currentDialogue = dialogue;
                        //AddTextToScroll(currentDialogue.CharacterId, currentDialogue.DialogueText);
                        proceed = true;
                    });
                }

                for (; dialogueIndex < dialogueOptionsParent.childCount; dialogueIndex++)
                {
                    GetTransformPooler(dialogueOptionsParent, dialogueIndex).gameObject.SetActive(false);
                }
            }

            yield return new WaitUntil(() => proceed);

        }

        //dialogueBox.SetActive(false);
        homeScreenButton.SetActive(true);
        disconnectSource.Play();
    }

    IEnumerator AddTextToScroll(string speaker, string text)
    {
        GameObject g = Instantiate(dialogueHolderParent.GetChild(0).gameObject, dialogueHolderParent);
        g.SetActive(true);
        TextMeshProUGUI textComponent = g.GetComponent<TextMeshProUGUI>();
        if (speaker == "Player")
        {
            speaker = "YOU";
            textComponent.color = new Color(0.1294118f, 0.145098f, 0.3882353f);
        }
        else
        {
            textComponent.color = Color.white;
            speaker = gameData.GetWizard(speaker).wizardNickName;
        }
        string finalText = $"<b>{speaker}:</b> {text}";
        textComponent.text = finalText;
        int letterCount = finalText.Length;
        float count = speaker.Length + 1;
        while (count < letterCount)
        {
            count += Time.deltaTime * gameData.lettersPerSecond;
            textComponent.maxVisibleCharacters = Mathf.FloorToInt(count);
            //Debug.Log($"Count: {count} {letterCount} {gameData.lettersPerSecond} {Time.deltaTime}");
            yield return null;
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                break;
            }
        }

        textComponent.maxVisibleCharacters = letterCount;
    }

    GameObject GetTransformPooler(Transform parent, int index)
    {
        if (index < parent.childCount)
        {
            return parent.GetChild(index).gameObject;
        }

        GameObject g = Instantiate(parent.GetChild(0), parent).gameObject;
        return g;
    }


    public void PlayVO(string trackName)
    {
        //Debug.Log("Requesting to Play VO");
        vOSource.Stop();
        //Debug.Log("Stopping previous VO");
        //Debug.Log($"Track name is {trackName}");
        if (string.IsNullOrEmpty(trackName)) return;
        vOSource.clip = gameData.GetClip(trackName);
        vOSource.Play();
    }
}
