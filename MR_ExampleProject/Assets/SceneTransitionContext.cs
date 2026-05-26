using UnityEngine;

/// <summary>
/// Survives scene loads. Remembers which scene and door the player used so paired doorways can return.
/// </summary>
public class SceneTransitionContext : MonoBehaviour
{
    public static SceneTransitionContext Instance { get; private set; }

    public string PreviousSceneName { get; private set; }
    public string EnteredThroughDoorId { get; private set; }

    bool m_ShouldFadeInOnLoad;

    public static SceneTransitionContext EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject(nameof(SceneTransitionContext));
        return go.AddComponent<SceneTransitionContext>();
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
    }

    public void SetExit(string fromScene, string doorId)
    {
        PreviousSceneName = fromScene;
        EnteredThroughDoorId = doorId;
        m_ShouldFadeInOnLoad = true;
    }

    public void Clear()
    {
        PreviousSceneName = null;
        EnteredThroughDoorId = null;
    }

    public bool ConsumeFadeInOnLoad()
    {
        if (!m_ShouldFadeInOnLoad)
            return false;

        m_ShouldFadeInOnLoad = false;
        return true;
    }
}
