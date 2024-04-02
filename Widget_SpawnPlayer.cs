using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Widget_SpawnPlayer : MonoBehaviour
{
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
}
