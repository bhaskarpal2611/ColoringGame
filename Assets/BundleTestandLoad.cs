using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BundleTestandLoad : MonoBehaviour
{

    public string bundleName;
      private AssetBundle assetBundle;
      private AsyncOperation asyncOperation;



    public bool isAndroid;

    private string filePathAssetBundle;

    public GameObject uiCategorySelectionPrefab;
    public Transform selectionCategoryHolder;

    public GameObject loadingObject;

    private Dictionary<int, RecentGameData> gameDataDict = new Dictionary<int, RecentGameData>();

    public string[] audiofolderName;
    private void Awake()
    {
     Screen.orientation = ScreenOrientation.LandscapeLeft;
        if (isAndroid)
        {

            filePathAssetBundle = Path.Combine(Application.streamingAssetsPath + "/android", bundleName);
        }
        else
        {



            filePathAssetBundle = Path.Combine(Application.streamingAssetsPath, bundleName);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void LoadSavedGameData()
    {
        List<RecentGameData> allData = UpdateCategoryApiManager.LoadAllGamePlayData();

        foreach (var data in allData)
        {
            gameDataDict[data.GameId] = data;
        }

        Debug.Log("Loaded Saved Games: " + gameDataDict.Count);
    }

    private void Start() {
        loadingObject.gameObject.SetActive(true);
        LoadSavedGameData();
        ReadAssetBundleSceneAndSpawnCategory();
    }
    private void ReadAssetBundleSceneAndSpawnCategory()
    {
        StartCoroutine(GetSceneNameFromBundle());
    }
    private IEnumerator GetSceneNameFromBundle()
    {

       AssetBundle.UnloadAllAssetBundles(true);
        string filePath = filePathAssetBundle;

        var assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(filePath);





        yield return assetBundleCreateRequest;





        string[] scenePaths = assetBundleCreateRequest.assetBundle.GetAllScenePaths();



        for (int i = 0; i < scenePaths.Length; i++)
        {
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePaths[i]);

            GameObject spawnedUIObject = Instantiate(uiCategorySelectionPrefab, selectionCategoryHolder);

             int index = i;
            spawnedUIObject.GetComponent<Button>().onClick.AddListener(()=> {
                    loadingObject.SetActive(true);
               StartCoroutine(RuntimeAudioLoader.Instance.CategoryAudioDownlaodAndLoader(audiofolderName[index]));
                LoadGameScene(index);
                PlayerPrefs.SetInt("currentGameId", index);
                //Debug.Log(" ajsdka " + audiofolderName[index]);

            });
            spawnedUIObject.transform.GetChild(1).GetComponent<TMP_Text>().text = sceneName;



            spawnedUIObject.name = sceneName;





            if (gameDataDict.ContainsKey(index))
            {
                RecentGameData data = gameDataDict[index];

                spawnedUIObject.transform.GetChild(2).GetComponent<TMP_Text>().text =
                 "Level: " + data.CompletedLevel + "/" + data.TotalLevel +
                "\nStars: " + data.Stars +
                "\nAttempts: " + data.Attempts +
                "\nTimeSpent In Second: " + data.TimeSpentInSeconds + "s";



            }
            else
            {
                spawnedUIObject.transform.GetChild(2).GetComponent<TMP_Text>().text = "No Data";
            }

        }
          loadingObject.SetActive(false);
    }

     public void LoadGameScene(int _sceneIndex)
        {
            //loadingObject.SetActive(true);
            StartCoroutine(LoadSceneFromBundle(_sceneIndex));
        //DEBUG DELETE LATER

//#if UNITY_EDITOR
        StartCoroutine(DEBUG_ReassingAllShaders());
//#endif

        //LoadSceneFromBundle(bundleName);
    }

#region MY DEBUG FUNCTION TO REASSIGN SHADER TO ALL MATERIALS IN SCENE BECAUSE OF BUG OF ASSETBUNDLE LOADING
//#if UNITY_EDITOR
    public IEnumerator DEBUG_ReassingAllShaders()
    {
        while (true)
        {

            yield return new WaitForSeconds(1f);// Buffer time Prob not needed...but whatev
            Renderer[] renderers = FindObjectsOfType<Renderer>(true);

            foreach (Renderer r in renderers)
            {
                foreach (Material mat in r.materials)
                {
                    var shaderName = mat.shader.name;
                    mat.shader = Shader.Find(shaderName);
                }
            }
            yield return null;
            //Sprite reassign
            SpriteRenderer[] sprites = FindObjectsOfType<SpriteRenderer>(true);

            foreach (SpriteRenderer sr in sprites)
            {
                sr.material.shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            }
            yield return null;
            //UI Image reassign
            Image[] images = FindObjectsOfType<Image>(true);

            foreach (Image img in images)
            {
                Material mat = img.material;

                if (mat != null)
                {
                    Shader shader = Shader.Find("UI/Default");

                    if (shader != null)
                    {
                        mat.shader = shader;
                    }
                }
            }
            TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);

            foreach (TMP_Text txt in texts)
            {
                if (txt.fontMaterial != null)
                {
                    txt.fontMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
                }
            }
        }
    }

    //#endif
    #endregion -----------------------------------------------------------------------------------------
    /*private IEnumerator LoadSceneFromBundle(int sceneIndex) OLD LOADER
    {
        AssetBundle.UnloadAllAssetBundles(true);



        string filePath = filePathAssetBundle;

        var assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(filePath);

        yield return assetBundleCreateRequest;



        assetBundle = assetBundleCreateRequest.assetBundle;
        string[] scenePaths = assetBundle.GetAllScenePaths();
        if (scenePaths.Length > 0)
        {
            //asyncOperation = SceneManager.LoadSceneAsync(scenePaths[0]);
            asyncOperation = SceneManager.LoadSceneAsync(scenePaths[sceneIndex]);
            // while (!asyncOperation.isDone)
            // {

            //     yield return null;
            // }
        }
        else
        {
            Debug.LogError("No scene found in the loaded AssetBundle.");
        }
        //loadingObject.SetActive(false);
        //  loadingObject.SetActive(false);
    }*/

    private IEnumerator LoadSceneFromBundle(int sceneIndex) // MY LAODER WITH BUG FIX OF SHADER REASSIGNMENT AFTER SCENE LOAD
    {
        AssetBundle.UnloadAllAssetBundles(true);

        string filePath = filePathAssetBundle;

        var assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(filePath);

        yield return assetBundleCreateRequest;

        assetBundle = assetBundleCreateRequest.assetBundle;

        string[] scenePaths = assetBundle.GetAllScenePaths();

        if (scenePaths.Length > 0)
        {
            asyncOperation = SceneManager.LoadSceneAsync(scenePaths[sceneIndex]);

            yield return asyncOperation;

            Debug.Log("Scene Loaded");

            yield return new WaitForSeconds(1f);

            StartCoroutine(DEBUG_ReassingAllShaders());
        }
    }
    public void ClosedApplication(){
            Application.Quit();
        }



    public void selectLangugae(string language){
        PlayerPrefs.SetString("PlayschoolLanguageAudio" , language);
          StartCoroutine(RuntimeAudioLoader.Instance.CategoryAudioDownlaodAndLoader("common",true));

}
}