using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Etienne.Text3D
{
    public class Text3D : MonoBehaviour
    {
        [SerializeField, Multiline(5)] private string text;
        [SerializeField] private Font3D font;
        [SerializeField] private Material material;

        [SerializeField] private float fontSize = 1f;
        [Header("Spacing Options")]
        [SerializeField] private float character = 1f;
        [SerializeField] private float word = 1f;
        [SerializeField] private float line = 1f;

        [SerializeField] private TextAlignment alignment;

        [SerializeField] private string oldText;

        [SerializeField] private List<Character3D> characters = new List<Character3D>();

        private async void OnValidate()
        {
            if(oldText != text) await UpdateText();
            Resize();
            PlaceText();
        }

        private async Task UpdateText()
        {
            font.Init();
            await DelayedClearEcess();
            text = text.ToUpper();
            string t = text;
            string[] lines = t.Split("\n");
            int globalIndex = 0;
            for(int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];

                string[] words = line.Split(' ');
                for(int wordIndex = 0; wordIndex < words.Length; wordIndex++)
                {
                    string word = words[wordIndex];
                    for(int charIndex = 0; charIndex < word.Length; charIndex++)
                    {
                        char c = word[charIndex];
                        Character3D goCharacter = GetCharacter(c, lineIndex, wordIndex, charIndex, globalIndex++);
                        goCharacter.transform.localPosition = Vector3.zero;

                        if(!font.TryGetMesh(c, out Mesh mesh))
                        {
                            continue;
                        }
                        goCharacter.filter.sharedMesh = mesh;
                    }
                }
            }
            oldText = text;
        }

        private Character3D GetCharacter(char c, int lineIndex, int wordIndex, int charIndex, int index)
        {
            if(index < characters.Count) return characters[index];

            GameObject go = new GameObject(c.ToString());
            go.transform.SetParent(transform);
            MeshFilter meshfilter = go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().material = material;
            Character3D character = new Character3D(lineIndex, wordIndex, charIndex, meshfilter);
            characters.Add(character);
            return character;
        }

        private void Resize()
        {
            foreach(Character3D c in characters)
            {
                c.transform.localScale = Vector3.one * fontSize;
            }
        }

        private void PlaceText()
        {
            if(alignment == TextAlignment.Left)
            {
                AlignLeft();
                return;
            }
            if(alignment == TextAlignment.Right)
            {
                AlignRight();
                return;
            }
            if(alignment == TextAlignment.Center)
            {
                AlignCenter();
                return;
            }
        }

        private void AlignCenter()
        {
            if(text == "") return;
            List<int> lineCharCounts = GetCharCountByLines();
            List<int> lineWordCounts = GetWordCountByLines();
            Vector3 position = Vector3.left * (lineCharCounts[0] * .5f) * character + Vector3.left * ((lineWordCounts[0]) * .5f) * word;
            int lineIndex = 0;
            int wordIndex = 0;
            foreach(Character3D c in characters)
            {
                if(!(c.characterIndex == 0 && c.wordIndex == 0 && c.lineIndex == 0)) position += character * Vector3.right;
                if(c.wordIndex > wordIndex)
                {
                    wordIndex++;
                    position += word * Vector3.right;
                }
                if(c.lineIndex > lineIndex)
                {
                    lineIndex++;
                    wordIndex = 0;
                    position = Vector3.down * lineIndex * line + Vector3.left * (lineCharCounts[lineIndex] * .5f) * character + Vector3.left * ((lineWordCounts[lineIndex]) * .5f) * word;
                }
                c.transform.localRotation = Quaternion.Euler(Vector3.up * 180);
                c.transform.localPosition = position * fontSize;
            }
        }

        private List<int> GetWordCountByLines()
        {
            List<int> lineCharCounts = new List<int>();
            int lineIndex = -1;
            int wordIndex = 0;
            foreach(Character3D character in characters)
            {
                if(lineCharCounts.Count == character.lineIndex)
                {
                    lineCharCounts.Add(0);
                    lineIndex++;
                }
                if(character.wordIndex > wordIndex)
                {
                    wordIndex++;
                    lineCharCounts[lineIndex]++;
                }
            }

            return lineCharCounts;
        }

        private List<int> GetCharCountByLines()
        {
            List<int> lineCharCounts = new List<int>();
            int lineIndex = -1;
            int wordIndex = 0;
            foreach(Character3D character in characters)
            {
                if(lineCharCounts.Count == character.lineIndex)
                {
                    lineCharCounts.Add(0);
                    lineIndex++;
                }
                if(character.wordIndex > wordIndex)
                {
                    wordIndex++;
                    lineCharCounts[lineIndex]++;
                }
                lineCharCounts[lineIndex]++;
            }

            return lineCharCounts;
        }

        private void AlignLeft()
        {
            Vector3 position = Vector3.zero;
            int lineIndex = 0;
            int wordIndex = 0;
            foreach(Character3D c in characters)
            {
                if(!(c.characterIndex == 0 && c.wordIndex == 0 && c.lineIndex == 0)) position += character * Vector3.right;
                if(c.wordIndex > wordIndex)
                {
                    wordIndex++;
                    position += word * Vector3.right;
                }
                if(c.lineIndex > lineIndex)
                {
                    lineIndex++;
                    wordIndex = 0;
                    position = Vector3.down * lineIndex * line;
                }
                c.transform.localRotation = Quaternion.Euler(Vector3.up * 180);
                c.transform.localPosition = position * fontSize;
            }
        }

        private void AlignRight()
        {
            Vector3 position = Vector3.zero;
            int lineIndex = -1;
            int wordIndex = -1;
            for(int i = characters.Count - 1; i >= 0; i--)
            {
                Character3D c = characters[i];

                position += character * Vector3.left;
                if(wordIndex == -1) wordIndex = c.wordIndex;
                if(c.wordIndex < wordIndex)
                {
                    wordIndex--;
                    position += word * Vector3.left;
                }
                if(lineIndex == -1) lineIndex = c.lineIndex + 1;
                if(c.lineIndex < lineIndex)
                {
                    lineIndex--;
                    wordIndex = -1;
                    position = Vector3.down * lineIndex * line;
                    Debug.Log(lineIndex + " " + position);
                }
                c.transform.localRotation = Quaternion.Euler(Vector3.up * 180);
                c.transform.localPosition = position * fontSize;
            }
        }

        private async Task DelayedClearEcess()
        {
            await Task.Delay(20);
            string noSpaceText = text.Replace(" ", "");
            if(noSpaceText.Length < characters.Count)
            {
                for(int i = characters.Count - 1; i >= noSpaceText.Length; i--)
                {
                    Character3D c = characters[i];
                    characters.Remove(c);
                    GameObject.DestroyImmediate(c.transform.gameObject);
                }
            }
        }

        [System.Serializable]
        private class Character3D
        {
            public int lineIndex, wordIndex, characterIndex;
            public MeshFilter filter;
            public Transform transform;
            public Character3D(int lineIndex, int wordIndex, int characterIndex, MeshFilter filter)
            {
                this.lineIndex = lineIndex;
                this.wordIndex = wordIndex;
                this.characterIndex = characterIndex;
                this.filter = filter;
                transform = filter.transform;
            }
        }
    }

}
