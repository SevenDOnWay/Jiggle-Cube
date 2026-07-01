using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Cube Color", menuName = "ScriptableObjects/CubeColorSO")]
public class CubeColorSO : ScriptableObject {

    [System.Serializable]
    public struct ColorConfig {
        public string id;
        public Color color;
    }

    public List<ColorConfig> cubeColors = new List<ColorConfig>();

}
