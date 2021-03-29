using UnityEngine;
using UnityEngine.UI;

namespace Terrain
{
    public class SaveLoadItem : MonoBehaviour
    {
        public SaveLoadMenu menu;
        private string _mapName;

        public string MapName {
            get => _mapName;
            set {
                _mapName = value;
                transform.GetChild(0).GetComponent<Text>().text = value;
            }
        }

        public void Select() {
            menu.SelectItem(_mapName);
        }
    }
}