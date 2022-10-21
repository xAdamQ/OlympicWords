using System.Collections.Generic;
using UnityEngine;

namespace Etienne.Text3D
{

    [CreateAssetMenu()]
    public class Font3D : ScriptableObject
    {
        [System.Serializable]
        public class Character3D
        {
            [field: SerializeField] public string FullName { get; private set; }
            [field: SerializeField] public char Character { get; private set; }
            [field: SerializeField] public Mesh Mesh { get; set; }

            public Character3D(char c)
            {
                Character = c;
                Mesh = null;
            }
            public Character3D(int i) : this((char)i) { }
            public Character3D(char c, string fullName) : this(c)
            {
                FullName = fullName;
            }

            public void SetMesh(Mesh mesh)
            {
                Mesh = mesh;
            }

            public override string ToString()
            {
                return $"{Character}, {Mesh}";
            }
        }
        [SerializeField] public List<Character3D> characters;
        private Dictionary<char, Mesh> meshes;

        private void Reset()
        {
            for(int i = 0; i < 26; i++)
            {
                characters.Add(new Character3D('A' + i));
            }
            for(int i = 0; i < 10; i++)
            {
                characters.Add(new Character3D('0' + i));
            }
            characters.Add(new Character3D(',', "Comma"));
            characters.Add(new Character3D('.', "Period"));
            characters.Add(new Character3D('!', "Exclamation"));
            characters.Add(new Character3D('?', "Question"));
            characters.Add(new Character3D('+', "Plus"));
            characters.Add(new Character3D('-', "Minus"));
            characters.Add(new Character3D('*', "Multiply"));
            characters.Add(new Character3D('/', "Divide"));
            characters.Add(new Character3D('%', "Percent"));
            characters.Add(new Character3D('=', "Equal"));
            characters.Add(new Character3D('@', "At"));
            characters.Add(new Character3D('#', "Hash"));
            characters.Add(new Character3D('$', "Dollar"));
            characters.Add(new Character3D('(', "OpenedParenthesis"));
            characters.Add(new Character3D(')', "ClosedParenthesis"));
        }


        public void Init()
        {
            if(meshes != null) return;
            meshes = new Dictionary<char, Mesh>();
            foreach(Character3D character in characters)
            {
                meshes.Add(character.Character, character.Mesh);
            }
        }

        public bool TryGetMesh(char c, out Mesh mesh)
        {
            return meshes.TryGetValue(c, out mesh);
        }


        public Mesh GetMesh(char c)
        {
            return meshes[c];
        }
    }
}
