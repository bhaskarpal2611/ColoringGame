using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.IO.Compression;

public class RuntimeAudioLoader : MonoBehaviour
{
    public enum Language
    {
        English,
        Tamil,
        Hindi,
        Telugu,
        Malayalam,
        Bengali,
        Gujarati,
        Marathi,
        Punjabi,
        Kannada,
        Odia,

        Russian,
        French,
        Spanish,
        German
    }

    public static RuntimeAudioLoader Instance;
    public AudioSource _commonAudioSource;
    Dictionary<string, AudioClip> audioDict = new Dictionary<string, AudioClip>();
    [SerializeField] private Language selectedLanguage;
    [SerializeField] string CurentSelectedLanguage;
    [SerializeField] string CurrentCategoryName;

    [Header("Local Testing Mode")]
    [Tooltip("Tick this to load audio directly from disk (already-downloaded files) without needing the other scene or an asset bundle.")]
    [SerializeField] private bool _localTestingMode = false;
    [Tooltip("Category folder name inside AudioBundles — e.g. 'identificationfruits'. Must match the folder on disk.")]
    [SerializeField] private string _localTestingCategory = "identificationfruits";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (_localTestingMode)
        {
            StartCoroutine(LoadLocalTestingAudio());
            return;
        }

        StartCoroutine(CategoryAudioDownlaodAndLoader("common", true));
    }

    // Loads common + the test category sequentially from already-saved disk files.
    // No download attempted — if the folder doesn't exist on disk it just warns and skips.
    private IEnumerator LoadLocalTestingAudio()
    {
        yield return StartCoroutine(CategoryAudioDownlaodAndLoader("common", true));
        yield return StartCoroutine(CategoryAudioDownlaodAndLoader(_localTestingCategory, false));
        Debug.Log($"[LocalMode] Finished loading '{_localTestingCategory}' from disk. audioDict={audioDict.Count} clips.");
    }

    public IEnumerator CategoryAudioDownlaodAndLoader(string currentCategory, bool isCommon = false)
    {
        CurrentCategoryName = currentCategory;
        string langStr = PlayerPrefs.GetString("PlayschoolLanguageAudio");

        if (System.Enum.TryParse(langStr, out selectedLanguage))
        {
            Debug.Log("Selected Language Enum: " + selectedLanguage);
        }
        else
        {
            Debug.LogWarning("Language not found, defaulting to English.");
            selectedLanguage = Language.English;
        }

        CurentSelectedLanguage = selectedLanguage.ToString();
        Debug.Log("Enum as String: " + CurentSelectedLanguage);
        yield return StartCoroutine(CheckDownloadExtract());
        StartCoroutine(LoadAllAudio(isCommon));
    }

    IEnumerator CheckDownloadExtract()
    {
        string bundleFolder = Path.Combine(Application.persistentDataPath, "AudioBundles");
        string categoryFolder = Path.Combine(bundleFolder, CurrentCategoryName);

        string zipPath = Path.Combine(categoryFolder, CurentSelectedLanguage + ".zip");
        string extractPath = Path.Combine(categoryFolder, CurentSelectedLanguage);

        if (!Directory.Exists(bundleFolder))
            Directory.CreateDirectory(bundleFolder);

        if (!Directory.Exists(categoryFolder))
            Directory.CreateDirectory(categoryFolder);

        if (!Directory.Exists(extractPath))
        {
            if (!File.Exists(zipPath))
            {
                yield return StartCoroutine(DownloadZip(zipPath));

                if (!File.Exists(zipPath))
                {
                    Debug.LogWarning("Zip not found on server for: " + CurentSelectedLanguage);
                    yield break;
                }
            }

            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                File.Delete(zipPath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Extraction failed: " + e.Message);
                yield break;
            }
        }
    }

    IEnumerator DownloadZip(string savePath)
    {
        string url = $"https://d2r38fn3ydtrfq.cloudfront.net/{CurrentCategoryName}/{CurentSelectedLanguage}.zip";

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.downloadHandler = new DownloadHandlerFile(savePath);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success || www.responseCode != 200)
        {
            Debug.LogError($"Download failed: {www.error} | Audio Data Not Found Subcategory Name {CurrentCategoryName} Language : {CurentSelectedLanguage} ");

            if (File.Exists(savePath))
                File.Delete(savePath);

            yield break;
        }
    }

    IEnumerator LoadAllAudio(bool isCommon)
    {
        // Capture before any yield — another coroutine can overwrite these class fields
        // the moment we suspend, causing the wrong folder to be read.
        string category = CurrentCategoryName;
        string language = CurentSelectedLanguage;

        if (!isCommon) audioDict.Clear();

        string path = Path.Combine(
            Application.persistentDataPath,
            "AudioBundles",
            category,
            language
        );

        if (!Directory.Exists(path))
        {
            Debug.LogWarning("Audio folder missing: " + path);
            yield break;
        }

        string[] files = Directory.GetFiles(path, "*.mp3");

        foreach (string file in files)
        {
            yield return LoadClip(file, isCommon);
        }
    }

    IEnumerator LoadClip(string filePath, bool isCommon)
    {
        UnityWebRequest www =
            UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("Clip load failed: " + filePath);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
        string key = Path.GetFileNameWithoutExtension(filePath);

        if (isCommon)
        {
            clip.name = key;
            CommonaudioDict[key] = clip;
        }
        else
        {
            clip.name = key;
            audioDict[key] = clip;
        }
    }

    public float PlayRuntimeAudio(string key)
    {
        AudioClip clip = GetClip(key);
        if (clip == null)
        {
            Debug.Log("Your key not Found Chal Chal " + key);
            return -1;
        }
        _commonAudioSource.Stop();
        _commonAudioSource.PlayOneShot(clip);

        return clip.length;
    }

    public AudioClip GetClip(string name)
    {
        if (audioDict.ContainsKey(name))
            return audioDict[name];

        Debug.LogWarning("Audio not found: " + name);
        return null;
    }

    public AudioClip GetCommonAudioClip(string name)
    {
        if (CommonaudioDict.ContainsKey(name))
            return CommonaudioDict[name];

        Debug.LogWarning("Audio not found: " + name);
        return null;
    }

    #region CommonAudio

    Dictionary<string, AudioClip> CommonaudioDict = new Dictionary<string, AudioClip>();

    public void PlayNumberClip(int number)
    {
        _commonAudioSource.Stop();
        _commonAudioSource.PlayOneShot(GetCommonAudioClip(number.ToString() + ".0"));
    }

    public void PlayAlphabetClip(string Alphabet)
    {
        _commonAudioSource.Stop();
        _commonAudioSource.PlayOneShot(GetCommonAudioClip(Alphabet));
    }

    public void PlayRetryAudioClip()
    {
        _commonAudioSource.Stop();
        string randoms = "retry" + UnityEngine.Random.Range(1, 7);
        _commonAudioSource.PlayOneShot(GetCommonAudioClip(randoms));
    }

    public void PlayNextLevelAudioClip()
    {
        _commonAudioSource.Stop();
        string randoms = "nextlevel" + UnityEngine.Random.Range(1, 7);
        _commonAudioSource.PlayOneShot(GetCommonAudioClip(randoms));
    }

    public void PlaytimesupAudioClip()
    {
        _commonAudioSource.Stop();
        string randoms = "timesup" + UnityEngine.Random.Range(1, 7);
        _commonAudioSource.PlayOneShot(GetCommonAudioClip(randoms));
    }

    public void PlayCorrectAudioClip()
    {
        _commonAudioSource.Stop();
        string randoms = "correct" + UnityEngine.Random.Range(1, 7);
        _commonAudioSource.PlayOneShot(GetCommonAudioClip(randoms));
    }

    public void PlayIncorrectAudioClip()
    {
        _commonAudioSource.Stop();
        string randoms = "incorrect" + UnityEngine.Random.Range(1, 7);
        _commonAudioSource.PlayOneShot(GetCommonAudioClip(randoms));
    }

    public void StopCommonAudioSource()
    {
        _commonAudioSource.Stop();
    }

    #endregion

    public IEnumerator DownlaodAllBatchZip()
    {
        string bundleFolder = Path.Combine(Application.persistentDataPath, "AudioBundles");
        string categoryFolder = Path.Combine(bundleFolder, CurrentCategoryName);

        foreach (Language lang in System.Enum.GetValues(typeof(Language)))
        {
            CurentSelectedLanguage = lang.ToString();
            Debug.Log("Enum as String: " + CurentSelectedLanguage);

            string zipPath = Path.Combine(categoryFolder, CurentSelectedLanguage + ".zip");
            string extractPath = Path.Combine(categoryFolder, CurentSelectedLanguage);

            if (!Directory.Exists(bundleFolder))
                Directory.CreateDirectory(bundleFolder);

            if (!Directory.Exists(categoryFolder))
                Directory.CreateDirectory(categoryFolder);

            if (!Directory.Exists(extractPath))
            {
                if (!File.Exists(zipPath))
                {
                    yield return StartCoroutine(DownloadZip(zipPath));

                    if (!File.Exists(zipPath))
                    {
                        Debug.LogWarning("Zip not found on server for: " + CurentSelectedLanguage);
                        yield break;
                    }
                }

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, extractPath);
                    File.Delete(zipPath);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Extraction failed: " + e.Message);
                    yield break;
                }
            }
        }
    }
}
