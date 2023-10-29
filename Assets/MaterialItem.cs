using UnityEngine;

namespace Aircraft
{
    public enum MaterialType
    {
        NoMaterial,
        Iron,
        Energy,
        Stone,
        Wood,
        Crystal
    }
    
    //[CreateAssetMenu(fileName = "MaterialItem", menuName = "MaterialItem")]
    public class MaterialItem : MonoBehaviour
    {
        [field:SerializeField] public MaterialType MaterialType { get; private set; }
        //[field:SerializeField] public Material Mesh { get; private set; }
    }    
}

