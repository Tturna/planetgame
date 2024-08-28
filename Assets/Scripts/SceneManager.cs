using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using unitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class SceneManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuParent;
    [SerializeField] private bool pauseOnFocusLost = true;
    
    [Header("Menu Page Stuff")]
    [FormerlySerializedAs("mainMenuPage"), SerializeField] private GameObject mainPage;
    [FormerlySerializedAs("mainSettingsPage"), SerializeField] private GameObject settingsPage;
    
    private static SceneManager _instance;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (pauseOnFocusLost && pauseMenuParent && !Application.isFocused && Time.timeScale > 0f)
        {
            PauseGame();
        }
        
        if (unitySceneManager.GetActiveScene().buildIndex != 1) return;

        if (pauseMenuParent && Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenuParent.activeSelf)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private static void LoadScene(int sceneIndex)
    {
        // unitySceneManager.LoadScene(sceneIndex);
        _instance.StartCoroutine(LoadSceneAsync(sceneIndex));
    }

    private static IEnumerator LoadSceneAsync(int sceneIndex)
    {
        var asyncLoad = unitySceneManager.LoadSceneAsync(sceneIndex);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        asyncLoad.allowSceneActivation = true;
    }
    
    public static void EnterGame()
    {
        Time.timeScale = 1f;
        LoadScene(1);
    }
    
    public static void EnterMainMenu()
    {
        Time.timeScale = 1f;
        LoadScene(0);
    }

    public static void QuitGame()
    {
        Application.Quit(0);
    }

    public static void PauseGame()
    {
        _instance.pauseMenuParent.SetActive(true);
        Time.timeScale = 0f;
    }

    public static void ResumeGame()
    {
        Time.timeScale = 1f;
        _instance.pauseMenuParent.SetActive(false);
    }

    public static void ShowSettingsPage()
    {
        _instance.mainPage.SetActive(false);
        _instance.settingsPage.SetActive(true);
    }

    public static void ShowMainPage()
    {
        _instance.mainPage.SetActive(true);
        _instance.settingsPage.SetActive(false);
    }
}
