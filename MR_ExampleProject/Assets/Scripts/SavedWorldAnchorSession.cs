using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Survives scene loads. Loads the same saved world anchor in each scene's ARAnchorManager.
/// </summary>
public class SavedWorldAnchorSession : MonoBehaviour
{
    public const string PlayerPrefsGuidKey = "SavedWorldAnchorGuid";

    public static SavedWorldAnchorSession Instance { get; private set; }

    public event Action<ARAnchor> AnchorReady;

    public bool IsReady { get; private set; }
    public ARAnchor LoadedAnchor { get; private set; }
    public string SavedAnchorGuid => m_SavedAnchorGuid;

    [SerializeField] string m_SavedAnchorGuid = "";
    [SerializeField] bool m_PersistGuidToPlayerPrefs = true;

    int m_LoadGeneration;
    bool m_LoadInProgress;

    public static SavedWorldAnchorSession EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject(nameof(SavedWorldAnchorSession));
        return go.AddComponent<SavedWorldAnchorSession>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (m_PersistGuidToPlayerPrefs && string.IsNullOrWhiteSpace(m_SavedAnchorGuid))
            m_SavedAnchorGuid = PlayerPrefs.GetString(PlayerPrefsGuidKey, string.Empty);

        UnitySceneManager.sceneUnloaded += OnSceneUnloaded;
        UnitySceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnitySceneManager.sceneUnloaded -= OnSceneUnloaded;
        UnitySceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    void OnSceneUnloaded(Scene scene)
    {
        ++m_LoadGeneration;
        m_LoadInProgress = false;
        IsReady = false;
        LoadedAnchor = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsReady = false;
        LoadedAnchor = null;

        if (!TryGetSavedGuid(out _))
            return;

        _ = LoadForCurrentSceneAsync();
    }

    public void ApplyInspectorGuid(string guid, bool persistGuidToPlayerPrefs)
    {
        m_PersistGuidToPlayerPrefs = persistGuidToPlayerPrefs;

        if (string.IsNullOrWhiteSpace(guid))
            return;

        if (!TryParseSerializableGuid(guid, out _))
        {
            Debug.LogError($"Invalid saved anchor GUID format: \"{guid}\".", this);
            return;
        }

        SetSavedGuid(guid);
    }

    public void CreateAndSaveWorldAnchor(Pose pose)
    {
        _ = CreateAndSaveWorldAnchorAsync(pose);
    }

    public void LoadSavedWorldAnchor()
    {
        _ = LoadForCurrentSceneAsync();
    }

    public void EraseSavedWorldAnchor()
    {
        _ = EraseSavedWorldAnchorAsync();
    }

    async Awaitable CreateAndSaveWorldAnchorAsync(Pose pose)
    {
        if (!TryFindAnchorManager(out var anchorManager))
            return;

        if (!anchorManager.descriptor.supportsSaveAnchor)
        {
            Debug.LogWarning("Save anchor is not supported on this device.", this);
            return;
        }

        await WaitForAnchorSubsystemAsync(anchorManager);

        var createResult = await anchorManager.TryAddAnchorAsync(pose);
        if (createResult.status.IsError())
        {
            Debug.LogError($"Failed to create world anchor: {createResult.status}", this);
            return;
        }

        var anchor = createResult.value;

        var saveResult = await anchorManager.TrySaveAnchorAsync(anchor);
        if (saveResult.status.IsError())
        {
            Debug.LogError($"Failed to save world anchor: {saveResult.status}", this);
            return;
        }

        SetSavedGuid(saveResult.value.ToString());
        MarkReady(anchor);
        Debug.Log($"World anchor saved. GUID: {m_SavedAnchorGuid}", this);
    }

    async Awaitable LoadForCurrentSceneAsync()
    {
        if (!TryGetSavedGuid(out var guid))
        {
            Debug.LogWarning("No saved world anchor GUID is configured.", this);
            return;
        }

        if (!TryFindAnchorManager(out var anchorManager))
            return;

        if (!anchorManager.descriptor.supportsLoadAnchor)
        {
            Debug.LogWarning("Load anchor is not supported on this device.", this);
            return;
        }

        var generation = ++m_LoadGeneration;
        m_LoadInProgress = true;
        IsReady = false;
        LoadedAnchor = null;

        await WaitForAnchorSubsystemAsync(anchorManager);

        if (generation != m_LoadGeneration)
            return;

        var loadResult = await anchorManager.TryLoadAnchorAsync(guid);
        if (generation != m_LoadGeneration)
            return;

        m_LoadInProgress = false;

        if (loadResult.status.IsError())
        {
            Debug.LogError($"Failed to load world anchor [{guid}]: {loadResult.status}", this);
            return;
        }

        MarkReady(loadResult.value);
        Debug.Log($"World anchor loaded for scene '{UnitySceneManager.GetActiveScene().name}'. GUID: {m_SavedAnchorGuid}", this);
    }

    async Awaitable EraseSavedWorldAnchorAsync()
    {
        if (!TryGetSavedGuid(out var guid))
        {
            Debug.LogWarning("No saved world anchor GUID is configured.", this);
            return;
        }

        if (!TryFindAnchorManager(out var anchorManager))
            return;

        if (!anchorManager.descriptor.supportsEraseAnchor)
        {
            Debug.LogWarning("Erase anchor is not supported on this device.", this);
            return;
        }

        await WaitForAnchorSubsystemAsync(anchorManager);

        var eraseResult = await anchorManager.TryEraseAnchorAsync(guid);
        if (eraseResult.IsError())
        {
            Debug.LogError($"Failed to erase world anchor [{guid}]: {eraseResult}", this);
            return;
        }

        if (LoadedAnchor != null)
        {
            anchorManager.TryRemoveAnchor(LoadedAnchor);
            LoadedAnchor = null;
        }

        ++m_LoadGeneration;
        m_LoadInProgress = false;
        IsReady = false;
        SetSavedGuid(string.Empty);
        Debug.Log("Saved world anchor erased.", this);
    }

    void MarkReady(ARAnchor anchor)
    {
        LoadedAnchor = anchor;
        IsReady = true;
        AnchorReady?.Invoke(anchor);
    }

    void SetSavedGuid(string guid)
    {
        m_SavedAnchorGuid = guid ?? string.Empty;

        if (!m_PersistGuidToPlayerPrefs)
            return;

        if (string.IsNullOrWhiteSpace(m_SavedAnchorGuid))
            PlayerPrefs.DeleteKey(PlayerPrefsGuidKey);
        else
            PlayerPrefs.SetString(PlayerPrefsGuidKey, m_SavedAnchorGuid);

        PlayerPrefs.Save();
    }

    bool TryFindAnchorManager(out ARAnchorManager anchorManager)
    {
        anchorManager = FindFirstObjectByType<ARAnchorManager>();
        if (anchorManager != null)
            return true;

        Debug.LogError("No ARAnchorManager found. Add one to your XR Origin.", this);
        return false;
    }

    static async Awaitable WaitForAnchorSubsystemAsync(ARAnchorManager anchorManager)
    {
        while (anchorManager.subsystem == null || !anchorManager.subsystem.running)
            await Awaitable.NextFrameAsync();
    }

    bool TryGetSavedGuid(out SerializableGuid guid)
    {
        guid = SerializableGuid.empty;

        if (string.IsNullOrWhiteSpace(m_SavedAnchorGuid))
            return false;

        if (TryParseSerializableGuid(m_SavedAnchorGuid, out guid) && guid != SerializableGuid.empty)
            return true;

        Debug.LogError($"Invalid saved anchor GUID format: \"{m_SavedAnchorGuid}\".", this);
        guid = SerializableGuid.empty;
        return false;
    }

    public static bool TryParseSerializableGuid(string text, out SerializableGuid guid)
    {
        guid = SerializableGuid.empty;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();

        if (Guid.TryParse(text, out var systemGuid))
        {
            guid = new SerializableGuid(systemGuid);
            return guid != SerializableGuid.empty;
        }

        var tokens = text.Split('-');
        if (tokens.Length != 2)
            return false;

        if (!ulong.TryParse(tokens[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var low))
            return false;

        if (!ulong.TryParse(tokens[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var high))
            return false;

        guid = new SerializableGuid(low, high);
        return guid != SerializableGuid.empty;
    }
}
