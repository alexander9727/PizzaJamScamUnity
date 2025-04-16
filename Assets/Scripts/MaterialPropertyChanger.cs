using UnityEngine;
using UnityEngine.UI;

public class MaterialPropertyChanger : MonoBehaviour
{
    [SerializeField] string propertyName;
    [SerializeField] float value;
    Material material;
    void Awake()
    {
        material = GetComponent<Image>().material;
        material = new Material(material);
        GetComponent<Image>().material = material;
    }

    // Update is called once per frame
    void Update()
    {
        if (material == null) return;
        if (string.IsNullOrEmpty(propertyName)) return;
        Debug.Log("Setting property");
        material.SetFloat(propertyName, value);
    }
}
