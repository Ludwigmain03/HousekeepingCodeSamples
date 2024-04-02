using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Invincibility))]
[RequireComponent(typeof(Movement))]
public class Player : NetworkBehaviour
{
    public int playerIdx;

    bool gameStarted;
    bool registered;

    public bool checkedTool;
    Movement move;
    Interact interact;
    NegativeEffects ne;
    Catchable catchScript;
    public Renderer mainRenderer;
    public Light spotlight;

    public Animator anim;
    GameManager gm;
    SORep repository;
    Canvas_Pause pauseScreen;
    Canvas_In_Game_UI cigu;
    Widget_SpawnPlayer spawnPlayer;
    CameraScript cameraScript;
    Transform cameraTransform;

    public Image classImage;
    public TMP_Text playerText;
    public eToolType[] toolType;
    public eToolType deployableTool;
    public Widget_PlayerInfo playerInfo;
    public eToolType toolInHand;
    public GameObject toolObject;
    GameObject deployableObject;

    Player[] colleagues;
    Player[] allPlayers;
    int numbColleagues;

    public eClassType playerClass;
    public int colorIdx;
    public eGremlinType gremlinType;
    public bool gremlin;
    [NamedArray(typeof(ePlayerComponent))] public MonoBehaviour[] vitalScripts;

    public ModelAnimHandler modelAnim;
    DecalProjector playerDecal;
    SpriteRenderer footSprite;
    Transform newHat;

    public int score;

    bool holding;
    bool deploying;
    public bool pausing;

    [Header("Utility Parameters")]
    [NamedArray(typeof(eClassType))] Sprite[] classSprites;
    [NamedArray(typeof(eGremlinType))] Sprite[] gremlinSprites;
    [NamedArray(typeof(eToolType))] Sprite[] toolSprites;
    [NamedArray(typeof(eToolType))] GameObject[] toolPrefabs;

    Widget_NullButton nullButton;

    public Transform head;
    public eHat myHat;
    public float hatDifference;
    public float hatScale = 1;
    public Collider triggerCollider;

    RaycastHit hit;
    CheckForTool currentToolCheck;

    float nameAppearanceOnStart;

    eController controllerUsing;

    public NetPlayerManager npm;

    void Awake()
    {

    }

    public void Start()
    {
        if(gm == null)
            StartUpActions();
    }

    public void StartUpActions()
    {
        for (int i = 0; i < vitalScripts.Length; i++)
        {
            if(vitalScripts[i] != null)
                vitalScripts[i].enabled = false;
        }

        pausing = false;
        move = GetComponent<Movement>();
        move.enabled = false;
        interact = GetComponent<Interact>();
        ne = GetComponent<NegativeEffects>();
        catchScript = GetComponent<Catchable>();
        cigu = FindObjectOfType<Canvas_In_Game_UI>();

        gm = GameManager.gm;
        repository = gm.soRep;

        classSprites = repository.classSprites;
        gremlinSprites = repository.gremlinSprites;
        toolSprites = repository.toolSprites;
        toolPrefabs = repository.toolPrefabs;

        cameraScript = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraScript>();
        cameraTransform = FindObjectOfType<Camera>().transform;

        if (gm.ready)
        {
            //Destroys this player if the game has already started
            Destroy(this.gameObject);
        }
        else if (!registered)
        {
            if (gm.spawners.Length == 0 || gm.spawners[0] == null)
            {
                gm.StartUpActions();
            }

            //Registers this object with spawn management
            playerIdx = gm.numberOfPlayers;
            transform.position = gm.transform.position + new Vector3(-1.5f * playerIdx, 0, -15);

            toolType = new eToolType[gm.set.totalToolSlots];
            toolInHand = eToolType.none;

            playerClass = eClassType.keeper;

            if (SceneManager.GetActiveScene().name != "FE")
            {
                ReportToGameManager();
            }

        }
    }

    private void OnSceneLoaded()
    {
        Debug.Log("Scene loaded");

        if (SceneManager.GetActiveScene().name != "FE")
        {
            ReportToGameManager();
        }
    }

    void ReportToGameManager()
    {
        gm.AddPlayer(this);
        spawnPlayer = gm.spawners[playerIdx];
        spawnPlayer.UpdatePlayerName();
        registered = true;
    }

    void FixedUpdate()
    {
        //updates to make a value false when a checktool is finished so no overlap occurs
        checkedTool = false;

        if (Physics.Raycast(cameraTransform.position, ((transform.position + Vector3.up * 0.25f) - cameraTransform.position), out hit, 1000))
        {
            playerText.gameObject.SetActive(!(hit.collider.gameObject == gameObject));
        }

        if(nameAppearanceOnStart > 0)
        {
            playerText.gameObject.SetActive(true);
            nameAppearanceOnStart -= Time.deltaTime;
        }
    }

    public void ShowInfo()
    {
        playerInfo.gameObject.SetActive(true);

        if (playerClass == eClassType.gremlin)
        {
            playerInfo.classImage.sprite = playerInfo.gremlinSprite[(int)gremlinType];
            playerInfo.background.color = gm.soRep.gremlinColors[colorIdx];
            playerInfo.nameBack.color = gm.soRep.gremlinColors[colorIdx];
            playerInfo.transform.SetParent(gm.canvasUI.scoreHolder[1]);

            gremlin = true;
        }
        else
        {
            playerInfo.classImage.sprite = playerInfo.KeeperSprite;
            playerInfo.background.color = gm.soRep.keeperColors[colorIdx];
            playerInfo.nameBack.color = gm.soRep.keeperColors[colorIdx];
            playerInfo.transform.SetParent(gm.canvasUI.scoreHolder[0]);

            gremlin = false;
        }
    }

    public void SetupUtility()
    {
        //Sets the player to gremlin if the class is of gremlin
        gremlin = (playerClass == eClassType.gremlin);

        //Instantiates the class' model and animation prefab and parents it to the player 
        if (modelAnim != null)
            Destroy(modelAnim.gameObject);

        modelAnim = Instantiate(gm.classObjects[(int)playerClass].coreModelAnim, transform.position, transform.rotation).GetComponent<ModelAnimHandler>();
        modelAnim.transform.parent = transform;
        mainRenderer = modelAnim.mainRenderer;
        anim = modelAnim.anim;
        move.GetModelAnimProperties();

        //Instantiates the foot sprite and makes its image into the sprite specified in the class object
        if (playerDecal == null)
        {
            playerDecal = Instantiate(Resources.Load("Widgets/" + "TEMP_player_ring") as GameObject, transform).GetComponent<DecalProjector>();
            playerDecal.transform.parent = transform;
            footSprite = playerDecal.GetComponentsInChildren<SpriteRenderer>()[0];
        }

        if (gremlin)
        {
            playerDecal.material = gm.soRep.gremlinRings[colorIdx];
            footSprite.sprite = gm.soRep.gremlinFootSprite[colorIdx];
        }
        else
        {
            playerDecal.material = gm.soRep.keeperRings[colorIdx];
            footSprite.sprite = gm.soRep.keeperFootSprite[colorIdx];
        }

        move.speed = gm.classObjects[(int)playerClass].speed;

        //RCE: Change model color to class indication color
        if (!gremlin)
        {
            modelAnim.skinnedRenderer.material = gm.soRep.keeperMats[colorIdx];
        }

        //Instantiates and parents the hat to the player's head
        if (myHat != eHat.none)
        {
            newHat = Instantiate(gm.soRep.hatPrefabs[(int)myHat], modelAnim.headTransform.position + new Vector3(0, hatDifference, 0), transform.rotation).transform;
            newHat.localScale = Vector3.one * modelAnim.transform.localScale.y;
            newHat.parent = modelAnim.headTransform;
        }

        GameObject poofEffect;
        if(gremlin)
            poofEffect = Instantiate(Resources.Load("GameObjects/" + "GremlinPoof") as GameObject, transform);
        else
            poofEffect = Instantiate(Resources.Load("GameObjects/" + "KeeperPoof") as GameObject, transform);

        nameAppearanceOnStart = 5;
        Destroy(poofEffect, 5);

        if (numbColleagues == 0)
            FindColleagues();
        /*
        for (int i = 0; i < numbColleagues; i++)
            colleagues[i].playerInfo.playerPoints.text = "" + ((cigu.progBar.maxValue / 2) / (numbColleagues)).ToString("F0");*/
    }

    public void ExternalSetup()
    {
        //Makes a line that shows which player is which
        UIPlayerLine line = Instantiate(Resources.Load("GameObjects/" + "UIPlayerLine") as GameObject).GetComponent<UIPlayerLine>();

        //Set position of the player
        if (gremlin && (gm.tm == null || gm.tm.allowOptions))
        {
            gremlinType = gm.classObjects[(int)playerClass].gremlinType;
            //colorIdx = gm.soRep.colors.Length - 1;

            Transform spawnPos = GameObject.FindGameObjectWithTag("GremlinSpawn").transform;
            transform.position = spawnPos.position + (spawnPos.right * -playerIdx);
            transform.rotation = spawnPos.rotation;

            line.Init(transform, playerInfo.transform, gm.soRep.gremlinColors[colorIdx], 5, true);

            //Player name and colors get established
            playerText.text = playerInfo.playerName.text;
            playerText.color = gm.soRep.gremlinColors[colorIdx];
        }
        else
        {
            if (gremlin)
                gremlinType = gm.classObjects[(int)playerClass].gremlinType;

            if (gm.tm == null)
                transform.position = gm.transform.position + new Vector3(-1f * playerIdx, 0, 0);
            else
            {
                if(gremlin)
                    transform.position = gm.tm.events[0].choreGroup.playerPos[1].position + (playerIdx * 0.5f * Vector3.right);
                else
                    transform.position = gm.tm.events[0].choreGroup.playerPos[0].position + (playerIdx * 0.5f * Vector3.right);
            }
                

            line.Init(transform, playerInfo.transform, gm.soRep.keeperColors[colorIdx], 5, true);

            //Player name and colors get established
            playerText.text = playerInfo.playerName.text;
            playerText.color = gm.soRep.keeperColors[colorIdx];
        }

        ShowInfo();

        //Set the camera up with the player's transform
        cameraScript.players[cameraScript.activePlayers] = transform;
        cameraScript.activePlayers++;
    }

    public void SwitchClass(eClassType _type)
    {
        if (interact.occupied)
            interact.Occupy();

        playerClass = _type;

        gremlin = (playerClass == eClassType.gremlin);

        if (gremlin)
        {
            gremlinType = gm.classObjects[(int)playerClass].gremlinType;
            playerText.color = gm.soRep.gremlinColors[colorIdx];
        }
        else
        {
            gremlinType = eGremlinType.none;
            playerText.color = gm.soRep.keeperColors[colorIdx];
        }

        SetupUtility();
        ShowInfo();
        ToggleScripting(true);
        interact.InitialSetup();
    }

    public void TogglePlaying(bool _playing)
    {
        ToggleScripting(_playing);
        if (!_playing)
        {
            move.speedReal = 0;
            anim.SetFloat("Speed", 0);
            move.ps.Stop();
        }
    }

    public bool IsPlaying()
    {
        return gameStarted;
    }

    public void ToggleScripting(bool _active)
    {
        if (_active)
            gameStarted = true;

        for (int i = 0; i < vitalScripts.Length; i++)
        {
            if (gm.classObjects[(int)playerClass].useableScripts[i])
            {
                vitalScripts[i].enabled = _active;
            }
            else
                vitalScripts[i].enabled = false;
        }
    }

    public void SetUp(eHat _hat, string _name)
    {
        //Sets up the player's overhead UI
        gremlin = (playerClass == eClassType.gremlin);

        myHat = _hat;
        playerInfo.playerName.text = _name;
        gremlinType = gm.classObjects[(int)playerClass].gremlinType;
        if(gremlin)
            playerInfo.classImage.sprite = playerInfo.gremlinSprite[(int)gremlinType];
        else
            playerInfo.classImage.sprite = playerInfo.KeeperSprite;
    }

    public void ChangePoints(int _points)
    {
        //Changes points value
        if(numbColleagues == 0)
            FindColleagues();

        score += _points;

        playerInfo.playerPoints.text = "" + score;

        if (gm.set.feedbackParticles)
        {
            GameObject tempFireworks = Instantiate(Resources.Load("GameObjects/" + "KeeperFeedback") as GameObject);
            tempFireworks.transform.position = transform.position;
            Destroy(tempFireworks, 10f);
        }

        PointIndicator pi = Instantiate(Resources.Load("GameObjects/" + "PointIndicator") as GameObject, transform).GetComponent<PointIndicator>();
        pi.Init(_points);
        interact.interactIcon.SetActive(false);
    }

    void FindColleagues()
    {
        allPlayers = FindObjectsOfType<Player>();
        numbColleagues = 0;
        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (allPlayers[i].gremlin == gremlin)
                numbColleagues++;
        }

        colleagues = new Player[numbColleagues];
        int colleagueIdx = 0;
        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (allPlayers[i].gremlin == gremlin)
            {
                colleagues[colleagueIdx] = allPlayers[i];
                colleagueIdx++;
            }
        }
    }

    public void OnSecondary()
    {
        if (pausing)
            pauseScreen.Back();
        else if (!gm.ready && !ne.dazed)
            spawnPlayer.Undo();
        else if (currentToolCheck != null && currentToolCheck.cancelable)
        {
            currentToolCheck.EndInteract();
            currentToolCheck = null;
        }
    }

    public void OnFire()
    {
        if (pausing)
            pauseScreen.Select();

        if (!gm.ready)
            spawnPlayer.Select();

        if (ne != null && ne.dazed || catchScript != null && catchScript.caught)
            return;

        bool notReallyOccupied = (interact.interact != null && (interact.interact.interactType == eInteractType.neutral || interact.interact.interactType == eInteractType.small));

        if ((!interact.occupied || notReallyOccupied) && !checkedTool)
        {
            int layerMask = 1 << 12;
            layerMask = ~layerMask;

            CheckForTool[] toolChecks = FindObjectsOfType<CheckForTool>();
            float tempDistance = 100;
            currentToolCheck = null;
            bool withinReasonableAngle = false;
            for (int i = 0; i < toolChecks.Length; i++)
            {
                Vector3 targetDir = (toolChecks[i].transform.position + (Vector3.up * 0.25f)) - (transform.position + (Vector3.up));
                GameObject collidedWith = null;
                if (Physics.Raycast(transform.position + Vector3.up, targetDir, out hit, 10f, layerMask))
                {
                    collidedWith = hit.collider.gameObject;
                }

                bool nothingInWay = (collidedWith == toolChecks[i].gameObject);
                float distance = Vector3.Distance(toolChecks[i].gameObject.transform.position, transform.position);
                bool checkIfKeeper = !gremlin && toolChecks[i].playerTypeRequired != eRequisite.gremlin;
                bool checkIfGremlin = gremlin && toolChecks[i].playerTypeRequired != eRequisite.keeper;
                bool canCheck = ((checkIfKeeper || checkIfGremlin) && toolChecks[i].gameObject != gameObject);

                targetDir.y = 0;
                float angle = Vector3.Angle(targetDir, transform.forward);
                if (angle > 180)
                    angle = Mathf.Abs(angle - 360);

                bool wra = angle < 45;

                if (toolChecks[i] != null && toolChecks[i].enabled && distance < 2 && distance < tempDistance && canCheck && wra && nothingInWay)
                {
                    tempDistance = distance;
                    currentToolCheck = toolChecks[i];

                    withinReasonableAngle = wra;
                }
            }
            if(currentToolCheck != null && withinReasonableAngle)
            {
                interact.buttonPrompt.gameObject.SetActive(false);
                currentToolCheck.CheckTool(gm.classObjects[(int)playerClass], gameObject);
            }
        }
    }

    public void OnJoin()
    {
        if(!gm.ready && !spawnPlayer.changingName)
            spawnPlayer.ReadyUp();
        else if (pausing)
        {
            pausing = false;
            pauseScreen.Resume();
            Time.timeScale = 1;
        }
        else if(gameStarted && vitalScripts[0].enabled)
        {
            pausing = true;
            pauseScreen = Instantiate(Resources.Load("Canvas/" + "Canvas_Pause") as GameObject).GetComponent<Canvas_Pause>();
            pauseScreen.Init(this);
            holding = false;
            Time.timeScale = 0;
        }
    }

    public void OnPlayStationSwitch()
    {
        controllerUsing = eController.pStation;
    }

    public void OnXBoxSwitch()
    {
        controllerUsing = eController.xBox;
    }

    public void OnKeyboardSwitch()
    {
        controllerUsing = eController.kBoard;
    }

    private void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();
        OnMove(movementVector);
    }

    public void OnMove(Vector2 _movementVector)
    {
        if (!gameStarted)
        {
            if (!holding)
            {
                if (Mathf.Abs(_movementVector.x) > Mathf.Abs(_movementVector.y))
                {
                    if (_movementVector.x > 0.1f)
                    {
                        spawnPlayer.Right(1);
                    }
                    else if (_movementVector.x < -0.1f)
                    {
                        spawnPlayer.Right(-1);
                    }
                }
                else
                {
                    if (_movementVector.y > 0.1f)
                    {
                        spawnPlayer.Up(-1);
                    }
                    else if (_movementVector.y < -0.1f)
                    {
                        spawnPlayer.Up(1);
                    }
                }
            }

            holding = (Mathf.Abs(_movementVector.x) > 0.1f || Mathf.Abs(_movementVector.y) > 0.1f);
        }
        else if (pausing)
        {
            if (!holding)
            {
                if (_movementVector.y > 0.1f)
                {
                    pauseScreen.Up(1);
                }
                else if (_movementVector.y < -0.1f)
                {
                    pauseScreen.Up(-1);
                }
            }
            holding = (Mathf.Abs(_movementVector.y) > 0.1f);
        }
    }

    public void SignalNull(int _buttonIdx)
    {
        if (nullButton != null)
            Destroy(nullButton.gameObject);

        nullButton = Instantiate(Resources.Load("Widgets/" + "Widget_NullButton") as GameObject).GetComponent<Widget_NullButton>();
        nullButton.transform.parent = transform;
        nullButton.transform.position = transform.position;
        nullButton.Init(_buttonIdx, controllerUsing);
    }

    public void ToggleDecal(bool _active)
    {
        playerDecal.enabled = _active;
    }

    public bool CanSpecialInput() 
    {
        return !interact.occupied;
    }

    public bool NoNearInteracts()
    {
        if (interact.occupied)
        {
            return false;
        }
        else
        {
            bool toolsNearby = false;
            CheckForTool[] toolChecks = FindObjectsOfType<CheckForTool>();
            for (int i = 0; i < toolChecks.Length; i++)
            {
                bool isNearby = (toolChecks[i] != null && toolChecks[i].playerTypeRequired != eRequisite.keeper && toolChecks[i].enabled && Vector3.Distance(toolChecks[i].gameObject.transform.position, transform.position) < 2);
                if (isNearby && toolChecks[i].gameObject != gameObject)
                {
                    Vector3 targetDir = toolChecks[i].transform.position - transform.position;
                    targetDir.y = 0;
                    float angle = Vector3.Angle(targetDir, transform.forward);
                    if (angle > 180)
                        angle = Mathf.Abs(angle - 360);
                    if (angle < 45)
                        toolsNearby = true;
                }
            }

            return !toolsNearby;
        }
    }

    public int GetControllerType()
    {
        return (int)controllerUsing;
    }
}