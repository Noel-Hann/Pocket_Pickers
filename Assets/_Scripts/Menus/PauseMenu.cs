using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused;
    public GameObject pauseMenuUI;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        InputHandler.Instance.OnPausePressed += HandlePauseEvent;
    }

    private void OnDisable()
    {
        if (InputHandler.Instance != null)
            InputHandler.Instance.OnPausePressed -= HandlePauseEvent;
    }

    private void HandlePauseEvent()
    {
        if (!IsPaused)
        {
            Pause();
            return;
        }
        Resume();
    }

    private void Pause()
    {
        // Do not allow the player to pause if they are dead
        if (GameManager.Instance is null) return;
        if (GameManager.Instance.isDead) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(GameManager.Instance.pauseMenuDefaultButton);
        
        pauseMenuUI.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
        IsPaused = true;
    }

    public void Resume()
    {
        // Do not allow the player to pause if they are dead
        if (GameManager.Instance is null) return;
        if (GameManager.Instance.isDead) return;
        
        pauseMenuUI.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void QuitToMainMenu()
    {
        // Application.Quit();
        
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        LevelLoader.Instance.LoadLevel(LevelLoader.Instance.menu);
        // SceneManager.LoadScene("MainMenuPlayTest2");
    }

    public void ResetSave()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        SaveManager.Instance.DeleteSaveFile();
        LevelLoader.Instance.LoadLevel(LevelLoader.Instance.map);
    }
}