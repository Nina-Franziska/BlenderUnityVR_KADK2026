using System.Collections;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class DoorSceneTrigger : MonoBehaviour
{
    [SerializeField] SceneManager sceneManager;
    [Tooltip("Stable id shared with the paired doorway in the forward scene (e.g. sceneA_to_sceneB).")]
    [SerializeField] string doorId;
    [Tooltip("The other scene at the end of this door (where you go when leaving the current scene).")]
    [SerializeField] string forwardSceneName;
    [Tooltip("Seconds after the MainCamera leaves the trigger before it can trigger again.")]
     float m_ExitResetDelay = 5f;

    bool m_HasTriggered;
    bool m_PendingSceneLoad;
    Coroutine m_ResetCoroutine;

    void Awake()
    {
        if (sceneManager == null)
            sceneManager = FindFirstObjectByType<SceneManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"OnTriggerEnter: {other.name}");
        if (!other.CompareTag("MainCamera"))
            return;

        CancelExitReset();

        if (m_HasTriggered)
            return;

        if (sceneManager == null)
        {
            Debug.LogWarning($"{nameof(DoorSceneTrigger)} on {name} has no SceneManager assigned.", this);
            return;
        }

        if (string.IsNullOrEmpty(forwardSceneName))
        {
            Debug.LogWarning($"{nameof(DoorSceneTrigger)} on {name} has no forward scene name.", this);
            return;
        }

        if (string.IsNullOrEmpty(doorId))
        {
            Debug.LogWarning($"{nameof(DoorSceneTrigger)} on {name} has no door id.", this);
            return;
        }

        string destination = GetDestinationScene();
        if (string.IsNullOrEmpty(destination))
        {
            Debug.LogWarning($"{nameof(DoorSceneTrigger)} on {name} could not resolve a destination scene.", this);
            return;
        }

        m_HasTriggered = true;
        m_PendingSceneLoad = true;
        string fromScene = UnitySceneManager.GetActiveScene().name;
        sceneManager.FadeBetweenLevels(false, () => LoadScene(destination, fromScene));
        Debug.Log($"FadeBetweenLevels: {destination}");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("MainCamera"))
            return;

        if (m_PendingSceneLoad)
            return;

        ScheduleExitReset();
    }

    void OnDisable()
    {
        CancelExitReset();
    }

    void ScheduleExitReset()
    {
        CancelExitReset();
        m_ResetCoroutine = StartCoroutine(ResetAfterExitDelay());
    }

    void CancelExitReset()
    {
        if (m_ResetCoroutine == null)
            return;

        StopCoroutine(m_ResetCoroutine);
        m_ResetCoroutine = null;
    }

    IEnumerator ResetAfterExitDelay()
    {
        yield return new WaitForSeconds(m_ExitResetDelay);
        m_HasTriggered = false;
        m_ResetCoroutine = null;
    }

    string GetDestinationScene()
    {
        var context = SceneTransitionContext.EnsureExists();

        // Return only when walking back through the same door you arrived from.
        if (context.EnteredThroughDoorId == doorId
            && context.PreviousSceneName == forwardSceneName)
        {
            return context.PreviousSceneName;
        }

        return forwardSceneName;
    }

    void LoadScene(string sceneName, string fromScene)
    {
        var context = SceneTransitionContext.EnsureExists();

        bool isReturn = sceneName == context.PreviousSceneName
            && context.EnteredThroughDoorId == doorId;

        if (isReturn)
            context.Clear();
        else
            context.SetExit(fromScene, doorId);

        UnitySceneManager.LoadScene(sceneName);
    }
}
