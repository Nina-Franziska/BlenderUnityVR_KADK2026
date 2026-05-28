using UnityEngine;

public class ColorChanger : MonoBehaviour
{

   [SerializeField] Material anchorMaterial;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anchorMaterial.color = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void ChangeColor()
    {
        anchorMaterial.color = Color.red;
    }
}
