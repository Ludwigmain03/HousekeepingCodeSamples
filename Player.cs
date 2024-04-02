using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
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
}
