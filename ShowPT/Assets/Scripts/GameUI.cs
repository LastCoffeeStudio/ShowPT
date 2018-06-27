﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour {

	[SerializeField]
	GameObject pauseScreen;

	Main mainManager;

	public void TogglePauseScreen()
	{
		if (CtrlPause.gamePaused == false) 
		{
			pauseScreen.SetActive(false);
            setCursorScreen(false);
		} 
		else 
		{
			pauseScreen.SetActive(true);
            setCursorScreen(true);
        }
	}

	public void GoBackToMain()
	{
        Application.Quit();
	}

    private void setCursorScreen(bool active)
    {
        if (active)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1;
        }
    }
}
