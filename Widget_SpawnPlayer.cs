using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Widget_SpawnPlayer : MonoBehaviour
{
    [Header("Player Information (Leave Empty)")]
    public bool active = false;
    public TMP_Text playerName;
    public TMP_Text playerType;
    public Color inactiveColor;
    public Image background;
    public int hatIdx;
    public TMP_Text hatText;
    public Player player;
    Widget_PlayerPedestal playerPedestal;
    int playerIdx;
    public bool gremlin;
    GameManager gm;
    bool tutorial;

    [Header("Ready Up Information")]
    public bool ready = false;
    public GameObject readyPrompt;
    public GameObject joinPrompt;
    public GameObject readyMark;
    public Transform[] UIPositions;
    public Transform selectionIcon;
    public Image buttonPromptImg;
    [NamedArray(typeof(eController))] public Sprite[] buttonSprites;
    int UIPosition;
    bool playerClassLocked;
    public TMP_Text startText;
    public Color[] stateColors;
    public string readyMessage;
    public string unreadyMessage;

    [Header("Naming UI")]
    public GameObject keyboard;
    public TMP_Text charLimitText;
    public Image[] letters;
    int focusedLetter;
    public bool changingName;
    bool changeNameStall;
    public int characterLimit = 5;
    int namelength;
    float shakeMagnitude;
    float shakeProgress;
    float nameOriginX;
    public Color normal;
    public Color highlight;
    public string savedName;

    [Header("Audio")]
    AudioSource source;
    public AudioClip change;
    public AudioClip select;
    public AudioClip back;

    public void Init(int _index)
    {
        gm = GameManager.gm;
        tutorial = gm.tm && !gm.tm.allowOptions;
        if (tutorial)
        {
            UIPositions[2].gameObject.SetActive(false);
            UIPositions[3].gameObject.SetActive(false);
        }
        readyPrompt.SetActive(false);
        changingName = false;
        selectionIcon.gameObject.SetActive(false);
        playerIdx = _index;
        playerName.text = "";
        background.color = inactiveColor;
        hatIdx = 0;
        startText.text = readyMessage;

        nameOriginX = playerName.transform.localPosition.x;

        source = GetComponent<AudioSource>();
    }

    public void UpdatePlayerName()
    {
        //Updates the player's name
        player.SetUp((eHat)hatIdx, playerName.text);
    }

    void FixedUpdate()
    {
        if(shakeMagnitude > 0)
        {
            shakeProgress += Time.deltaTime * 35;
            shakeMagnitude -= Time.deltaTime;
            float newPos = nameOriginX + shakeMagnitude * 15 * Mathf.Sin(shakeProgress);
            playerName.transform.localPosition = new Vector3(newPos, playerName.transform.localPosition.y, 0);
            charLimitText.transform.localScale = Vector3.one * (1 + (shakeMagnitude * 0.5f));
        }

        if(player != null)
            buttonPromptImg.sprite = buttonSprites[player.GetControllerType()];
    }

    public void SetUpPlayer()
    {
        //Get players set up when they first enter the game
        active = true;
        selectionIcon.gameObject.SetActive(true);
        readyPrompt.SetActive(true);
        joinPrompt.SetActive(false);

            player.colorIdx = playerIdx - 1;
            if ((playerIdx) % 2 == 0)
            {
                player.playerClass = eClassType.gremlin;
                gremlin = true;
            }
            playerName.text = "Player " + (playerIdx + 1);

        if (gremlin)
            player.playerInfo = Instantiate(Resources.Load("Widgets/" + "Widget_PlayerInfo") as GameObject, gm.canvasUI.scoreHolder[1]).GetComponent<Widget_PlayerInfo>();
        else
            player.playerInfo = Instantiate(Resources.Load("Widgets/" + "Widget_PlayerInfo") as GameObject, gm.canvasUI.scoreHolder[0]).GetComponent<Widget_PlayerInfo>();
        player.SetUp((eHat)hatIdx, playerName.text);

        playerPedestal = gm.cReadyUpScene.playerPedestals[playerIdx];
        playerPedestal.ShowInfo(playerIdx);
        ChangeHat();
        playerPedestal.SetHat(hatIdx);
        ToggleClass();
        //PlayerPrefs.SetInt("SavedOptions" + playerIdx, 1);

        gm.CheckReady();

        source.PlayOneShot(select);
    }

    public void ReadyUp()
    {
        //Toggle readiness and communicate the new state with the GameManager
        ready = !ready;
        readyMark.SetActive(ready);
        gm.CheckReady();
        selectionIcon.gameObject.SetActive(!ready);
        if (ready)
        {
            startText.text = unreadyMessage;
            startText.color = stateColors[0];

            source.PlayOneShot(select);
        }
        else
        {
            startText.text = readyMessage;
            startText.color = stateColors[1];
        }
    }

    public void ChangeName(bool _active)
    {
        //Toggles whether the player is changing their name or not
        changingName = _active;
        keyboard.SetActive(changingName);
        charLimitText.gameObject.SetActive(changingName);
        if (changingName)
        {
            //Resets name if longer than character limit
            if(playerName.text.Length > characterLimit)
            {
                playerName.text = "";
            }

            namelength = playerName.text.Length;
            charLimitText.text = namelength + "/" + characterLimit;
        }
        else
        {
            //If name is left blank when closing
            if(playerName.text == "")
                playerName.text = "Player " + (playerIdx + 1);

            player.playerInfo.playerName.text = playerName.text;
            /*
            PlayerPrefs.SetString("NameOption" + playerIdx, playerName.text);
            Debug.Log(PlayerPrefs.GetString("NameOption" + playerIdx));*/
        }

        int tempFocus = focusedLetter;
        focusedLetter = 0;
        FocusLetter(tempFocus);
    }

    public void Up(int dir)
    {
        if (ready)
            return;

        if (changingName)
        {
            //Gets up and down input from player for Name UI
            int tempFocus = focusedLetter;
            focusedLetter += (dir * 6);
            FocusLetter(tempFocus);

            source.PlayOneShot(change);
        }
        else
        {
            UIPosition += dir;
            if (UIPosition >= UIPositions.Length || UIPosition > 1 && playerClassLocked)
                UIPosition = 0;
            else if (UIPosition < 0)
            {
                if (playerClassLocked)
                    UIPosition = 1;
                else
                    UIPosition = (UIPositions.Length - 1);
            }

            source.PlayOneShot(change);
            selectionIcon.position = UIPositions[UIPosition].position;
        }
    }

    public void Right(int dir)
    {
        if (changingName)
        {
            //Gets right and left input from player for Name UI
            int tempFocus = focusedLetter;
            focusedLetter += dir;
            FocusLetter(tempFocus);

            source.PlayOneShot(change);
        }
    }

    void FocusLetter(int _prevLetter)
    {
        //Changes the letter being focused on depending on Up or Right function
        if (focusedLetter < 0)
            focusedLetter = 0;
        else if (focusedLetter >= letters.Length)
            focusedLetter = (letters.Length - 1);

        letters[_prevLetter].color = normal;
        letters[focusedLetter].color = highlight;
    }

    public void Select()
    {
        if (ready)
            return;

        //Adds a letter to the name string or removes a letter if on the minus button
        if (changingName)
        {
            if (focusedLetter == 28) // Player selects enter to finalize name
            {
                ChangeName(false);
                changeNameStall = true;

                source.PlayOneShot(select);
            }
            else if (focusedLetter == 27 && namelength > 0) // Player selects delete to remove a character 
            {
                playerName.text = playerName.text.Substring(0, playerName.text.Length - 1);

                source.PlayOneShot(back);
            }
            else if (focusedLetter == 26 && namelength < characterLimit) // Player selects space
            {
                playerName.text += " ";

                source.PlayOneShot(select);
            } 
            else if (namelength < characterLimit && focusedLetter != 27) // Player selects any other character
            { 
                playerName.text += letters[focusedLetter].name;

                source.PlayOneShot(select);
            }
            else
            {
                shakeMagnitude = 0.7f;

                source.PlayOneShot(back);
            }

            namelength = playerName.text.Length;
            charLimitText.text = namelength + "/" + characterLimit;
        }

        if (!changingName)
            source.PlayOneShot(select);

        switch (UIPosition) 
        {
            case 0:
                if (!changingName && !changeNameStall)
                {
                    savedName = playerName.text;
                    ChangeName(!changingName);
                }
                changeNameStall = false;
                break;
            case 1:
                ChangeHat();
                break;
            case 2:
                ChangeColor();
                break;
            case 3:
                ToggleClass();
                break;
        }
    }

    public void Undo()
    {
        if (changingName)
        {
            //playerName.text = savedName;
            ChangeName(false);

            source.PlayOneShot(back);
        }
    }

    void ChangeHat()
    {
        hatIdx++;
        if (hatIdx >= gm.soRep.hatPrefabs.Length)
            hatIdx = 0;

        if(gm.spawners.Length > 1)
        {
            for (int i = 0; i < gm.spawners.Length; i++)
            {
                if (gm.spawners[i].active && hatIdx == gm.spawners[i].hatIdx && i != playerIdx || gm.soRep.hatPrefabs[hatIdx] == null)
                {
                    ChangeHat();
                    return;
                }
            }
        }

        player.myHat = (eHat)hatIdx;

        hatText.text = "Hat: " + player.myHat.ToString();
        playerPedestal.SetHat(hatIdx);
        player.SetUp((eHat)hatIdx, playerName.text);
    }

    void ChangeColor()
    {
        if (playerClassLocked)
            return;

        int colorIdx = player.colorIdx + 1;
        if (colorIdx >= gm.soRep.keeperColors.Length)
            colorIdx = 0;

        bool isADuplicate = false;

        if (gm.spawners.Length > 1)
        {
            for (int i = 0; i < gm.spawners.Length; i++)
            {
                if (gm.players[i] != null && gm.players[i].colorIdx == colorIdx && gm.players[i] != player && player.playerClass == gm.players[i].playerClass)
                    isADuplicate = true;
            }
        }

        player.colorIdx = colorIdx;

        if (isADuplicate)
        {
            ChangeColor();
            return;
        }

        playerPedestal.SetColor(colorIdx, player.playerClass == eClassType.gremlin);
        PlayerPrefs.SetInt("ColorOption" + playerIdx, player.colorIdx);

        SetColor();
    }

    void ToggleClass()
    {
        if (playerClassLocked)
            return;

        gremlin = !gremlin;

        if (gremlin)
        {
            player.playerClass = eClassType.gremlin;
        }
        else
        {
            player.playerClass = eClassType.keeper;
        }

        playerType.text = gm.classObjects[(int)player.playerClass].realName;
        ChangeColor();

        playerClassLocked = ((PlayerPrefs.GetInt("SelectionMode") == 1) || tutorial);
    }

    void SetColor()
    {
        if (PlayerPrefs.GetInt("SelectionMode") < 3)
        {
            if (player.playerClass == eClassType.gremlin)
                background.color = gm.soRep.gremlinColors[player.colorIdx];
            else
                background.color = gm.soRep.keeperColors[player.colorIdx];
        }
        else
            background.color = Color.white;
    }
}
