using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SceneManager : MonoBehaviour
{
    [SerializeField] Material fadeMaterial;
    public float fadeSpeed = 3f;

    Coroutine m_FadeCoroutine;
    Action m_OnFadeComplete;

    void Start()
    {

        fadeMaterial.SetFloat("_Alpha", 0f);
        //     fadeMaterial.SetFloat("_Alpha", 1f);
        //     FadeBetweenLevels(true);
        // if (fadeMaterial == null)
        //     return;

        var context = SceneTransitionContext.Instance;
        // if (context != null && context.ConsumeFadeInOnLoad())
        // {
        //     fadeMaterial.SetFloat("_Alpha", 1f);
        //     FadeBetweenLevels(true);
        //     return;
        // }

       // fadeMaterial.SetFloat("_Alpha", 0f);
    }


    void Update()
    {
       
    }


    void FadeTester()
    {
         if (Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            FadeBetweenLevels(true);
        else if (Keyboard.current.spaceKey.wasReleasedThisFrame)
            FadeBetweenLevels(false);
    }

    public void FadeBetweenLevels(bool visible, Action onComplete = null)
    {
        if (fadeMaterial == null)
        {
            onComplete?.Invoke();
            Debug.Log("FadeMaterial is null");
            return;
        }

        m_OnFadeComplete = onComplete;

        if (m_FadeCoroutine != null)
            StopCoroutine(m_FadeCoroutine);

        m_FadeCoroutine = StartCoroutine(Fade(visible));
    }

    IEnumerator Fade(bool visible)
    {
        float alphaValue = fadeMaterial.GetFloat("_Alpha");

        if (visible)
        {
            while (fadeMaterial.GetFloat("_Alpha") > 0f)
            {
                alphaValue -= Time.deltaTime / fadeSpeed;
                fadeMaterial.SetFloat("_Alpha", alphaValue);
                yield return null;
            }

            fadeMaterial.SetFloat("_Alpha", 0f);
        }
        else
        {
            while (fadeMaterial.GetFloat("_Alpha") < 1f)
            {
                alphaValue += Time.deltaTime / fadeSpeed;
                fadeMaterial.SetFloat("_Alpha", alphaValue);
                yield return null;
            }

            fadeMaterial.SetFloat("_Alpha", 1f);
        }

        m_FadeCoroutine = null;

        var callback = m_OnFadeComplete;
        m_OnFadeComplete = null;
        callback?.Invoke();
    }
}
