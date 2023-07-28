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

    [Header("Ready Up Information")]
    public bool ready = false;
    public GameObject readyPrompt;
    public GameObject readyMark;
    public Transform[] UIPositions;
    public Transform selectionIcon;
    int UIPosition;
    bool playerClassLocked;
    public TMP_Text startText;
    public Color[] stateColors;
    public string readyMessage;
    public string unreadyMessage;

    [Header("Naming UI")]
    public GameObject keyboard;
    public Image[] letters;
    int focusedLetter;
    public bool changingName;
    bool changeNameStall;
    public int characterLimit = 5;
    int namelength;
    public Color normal;
    public Color highlight;
    public string savedName;

    public void Init(int _index)
    {
        gm = GameManager.gm;
        if(gm.tm != null)
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
    }

    public void UpdatePlayerName()
    {
        //Updates the player's name
        player.SetUp((eHat)hatIdx, playerName.text);
    }

    public void SetUpPlayer()
    {
        //Get players set up when they first enter the game
        active = true;
        selectionIcon.gameObject.SetActive(true);
        readyPrompt.SetActive(true);
        gm.cReadyUpScene.startPrompt.SetActive(false);

        //Get saved player options from PlayerPrefs
        bool savedOptions = PlayerPrefs.GetInt("SavedOptions" + playerIdx) == 1;
        if (savedOptions)
        {
            playerName.text = PlayerPrefs.GetString("NameOption" + playerIdx);
            hatIdx = PlayerPrefs.GetInt("HatOption" + playerIdx) - 1;
        }

        if (gm.tm == null && savedOptions)
        {
            player.colorIdx = PlayerPrefs.GetInt("ColorOption" + playerIdx) - 1;
            gremlin = !(PlayerPrefs.GetInt("GremlinOption" + playerIdx) == 1);
        }
        else 
        {
            player.colorIdx = playerIdx - 1;
            if ((playerIdx) % 2 == 0)
            {
                player.playerClass = eClassType.gremlin;
                gremlin = true;
            }
            playerName.text = "Player " + (playerIdx + 1);
        }

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
        PlayerPrefs.SetInt("SavedOptions" + playerIdx, 1);

        gm.CheckReady();
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
        if (changingName)
        {
            //Resets name if longer than character limit
            if(playerName.text.Length > characterLimit)
                playerName.text = "";

            namelength = playerName.text.Length;
        }
        else
        {
            //If name is left blank when closing
            if(playerName.text == "")
                playerName.text = "Player " + (playerIdx + 1);

            player.playerInfo.playerName.text = playerName.text;
            PlayerPrefs.SetString("NameOption" + playerIdx, playerName.text);
            Debug.Log(PlayerPrefs.GetString("NameOption" + playerIdx));
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
            }
            else if (focusedLetter == 27 && namelength > 0) // Player selects delete to remove a character 
                playerName.text = playerName.text.Substring(0, playerName.text.Length - 1);
            else if (focusedLetter == 26 && namelength < characterLimit) // Player selects space
                playerName.text += " ";
            else if(namelength < characterLimit) // Player selects any other character
                playerName.text += letters[focusedLetter].name;

            namelength = playerName.text.Length;
        }

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
            playerName.text = savedName;
            ChangeName(false);
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

        PlayerPrefs.SetInt("HatOption" + playerIdx, hatIdx);
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
            PlayerPrefs.SetInt("GremlinOption" + playerIdx, 1);
            player.playerClass = eClassType.gremlin;
        }
        else
        {
            PlayerPrefs.SetInt("GremlinOption" + playerIdx, 0);
            player.playerClass = eClassType.keeper;
        }

        playerType.text = gm.classObjects[(int)player.playerClass].realName;
        ChangeColor();

        playerClassLocked = ((PlayerPrefs.GetInt("SelectionMode") == 1) || gm.tm != null);
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
