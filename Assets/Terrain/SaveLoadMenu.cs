using System;
using System.IO;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;

namespace Terrain
{
    public class SaveLoadMenu : MonoBehaviour
    {
        public HexGrid hexGrid;
        public Text menuLabel, actionButtonLabel;
        public InputField nameInput;
        public RectTransform listContent;
        public SaveLoadItem itemPrefab;
        private bool _saveMode;

        public void Open(bool saveMode) {
            _saveMode = saveMode;
            if (saveMode) {
                menuLabel.text = "Save Map";
                actionButtonLabel.text = "Save";
            }
            else {
                menuLabel.text = "Load Map";
                actionButtonLabel.text = "Load";
            }

            FillList();
            gameObject.SetActive(true);
            HexMapCamera.Locked = true;
        }

        public void Close() {
            gameObject.SetActive(false);
            HexMapCamera.Locked = false;
        }

        private string GetSelectedPath() {
            var mapName = nameInput.text;
            if (string.IsNullOrWhiteSpace(mapName)) return null;

            return Path.Combine(Application.persistentDataPath, mapName + ".map");
        }

        public void Save(string path) {
            using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
            writer.Write(1);
            hexGrid.Save(writer);
        }

        public void Load(string path) {
            if (!File.Exists(path)) {
                Debug.LogError("File does not exist: " + path);
                return;
            }

            using var reader = new BinaryReader(File.OpenRead(path));
            var header = reader.ReadInt32();
            if (header <= 1) {
                hexGrid.Load(reader, header);

                HexMapCamera.ValidatePosition();
            }
            else {
                Debug.LogWarning("Unknown map format " + header);
            }
        }

        public void Action() {
            var path = GetSelectedPath();
            if (path == null) return;
            if (_saveMode) {
                Save(path);
            }
            else {
                Load(path);
            }

            Close();
        }

        public void Delete() {
            var path = GetSelectedPath();
            if (path == null) return;

            if (File.Exists(path)) {
                File.Delete(path);
            }

            nameInput.text = "";
            FillList();
        }

        public void SelectItem(string name) {
            nameInput.text = name;
        }

        private void FillList() {
            for (var i = 0; i < listContent.childCount; i++) {
                Destroy(listContent.GetChild(i).gameObject);
            }

            var paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
            Array.Sort(paths);
            foreach (var path in paths) {
                var item = Instantiate(itemPrefab, listContent, false);
                item.menu = this;
                item.MapName = Path.GetFileNameWithoutExtension(path);
            }
        }
    }
}