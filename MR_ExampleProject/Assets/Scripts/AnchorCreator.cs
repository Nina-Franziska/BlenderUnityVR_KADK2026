using UnityEngine;
using UnityEngine.InputSystem;

public class AnchorCreator : MonoBehaviour
{
    public InputActionReference AnchorAction;
    public SavedWorldAnchorSetup SavedWorldAnchorSetup;

    // UnityEvent OnAnchorCreated;
    //TO DO __ INSER THE WORLD ANCHORING PREFAB INTO ALL SCENES 
    public void CreateAnchor()
    {
        Debug.Log("CreateAnchor");
        SavedWorldAnchorSetup.CreateAndSaveWorldAnchor();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // AnchorAction.action.performed += CreateAnchor;
    }

    // Update is called once per frame
    void Update()
    {

        if (AnchorAction.action.triggered)
        {
            CreateAnchor();
        }

    }
}
