using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Per-scene hook that parents exhibition content under the shared saved world anchor.
/// Add one to each scene with a Content Root assigned. Anchor create/load/erase is handled by
/// <see cref="SavedWorldAnchorSession"/>, which survives scene loads.
/// </summary>
public class SavedWorldAnchorSetup : MonoBehaviour
{
    [Tooltip("Pose used when creating a new anchor. Defaults to this transform.")]
    [SerializeField] Transform m_AnchorOrigin;
    [Tooltip("Exhibition content in this scene that should follow the loaded anchor.")]
    [SerializeField] Transform m_ContentRoot;
    [Tooltip("Optional GUID override. Usually left empty so PlayerPrefs is used.")]
    [SerializeField] string m_SavedAnchorGuid = "";
    [SerializeField] bool m_LoadOnStart = true;
    [SerializeField] bool m_PersistGuidToPlayerPrefs = true;
    [SerializeField] UnityEvent m_OnAnchorReady;

    SavedWorldAnchorSession m_Session;

    public bool IsReady => m_Session != null && m_Session.IsReady;
    public ARAnchor LoadedAnchor => m_Session != null ? m_Session.LoadedAnchor : null;
    public string SavedAnchorGuid => m_Session != null ? m_Session.SavedAnchorGuid : m_SavedAnchorGuid;

    void Awake()
    {
        if (m_AnchorOrigin == null)
            m_AnchorOrigin = transform;

        if (m_ContentRoot == null)
            m_ContentRoot = transform;

        m_Session = SavedWorldAnchorSession.EnsureExists();

        if (!string.IsNullOrWhiteSpace(m_SavedAnchorGuid))
            m_Session.ApplyInspectorGuid(m_SavedAnchorGuid, m_PersistGuidToPlayerPrefs);
    }

    void OnEnable()
    {
        if (m_Session == null)
            m_Session = SavedWorldAnchorSession.EnsureExists();

        m_Session.AnchorReady += HandleAnchorReady;

        if (m_Session.IsReady)
            HandleAnchorReady(m_Session.LoadedAnchor);
    }

    void OnDisable()
    {
        if (m_Session != null)
            m_Session.AnchorReady -= HandleAnchorReady;
    }

    [ContextMenu("Create And Save World Anchor")]
    public void CreateAndSaveWorldAnchor()
    {
        var session = SavedWorldAnchorSession.EnsureExists();
        var origin = m_AnchorOrigin != null ? m_AnchorOrigin : transform;
        session.CreateAndSaveWorldAnchor(new Pose(origin.position, origin.rotation));
    }

    [ContextMenu("Load Saved World Anchor")]
    public void LoadSavedWorldAnchor()
    {
        SavedWorldAnchorSession.EnsureExists().LoadSavedWorldAnchor();
    }

    [ContextMenu("Erase Saved World Anchor")]
    public void EraseSavedWorldAnchor()
    {
        SavedWorldAnchorSession.EnsureExists().EraseSavedWorldAnchor();
    }

    void HandleAnchorReady(ARAnchor anchor)
    {
        if (!m_LoadOnStart || anchor == null)
            return;

        ParentContentToAnchor(anchor.transform);
        m_OnAnchorReady?.Invoke();
    }

    void ParentContentToAnchor(Transform anchorTransform)
    {
        if (m_ContentRoot == null || anchorTransform == null)
            return;

        m_ContentRoot.SetParent(anchorTransform, false);
        m_ContentRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
}
