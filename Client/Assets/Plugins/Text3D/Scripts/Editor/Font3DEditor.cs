using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EtienneEditor
{
    public class Editor<T> : Editor where T : class
    {
        protected T Target => target as T;

        protected void ForceSceneUpdate()
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}

namespace EtienneEditor.Text3D
{
    using Etienne.Text3D;
    using System.Collections.Generic;
    using System.Linq;

    [CustomEditor(typeof(Font3D))]
    public class Font3DEditor : Editor<Font3D>
    {
        private VisualElement root;
        private Dictionary<char, ObjectField> fields;

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            fields = new Dictionary<char, ObjectField>();

            Button button = new Button(FillWithSelection)
            {
                text = "Fill with selected Objects"
            };
            root.Add(button);
            DrawAlphabetFields();
            return root;
        }

        private void FillWithSelection()
        {
            foreach (Object item in Selection.objects)
            {
                if (!(item is Mesh || item is GameObject)) continue;
                if (item.name[^2] != '_') continue;
                char c = item.name.ToUpper()[^1];
                if (char.IsLetterOrDigit(c))
                {
                    if (item is Mesh mesh)
                    {
                        fields[c].value = mesh;
                        continue;
                    }

                    if (item is GameObject go && go.TryGetComponent(out MeshFilter filter))
                    {
                        fields[c].value = filter.sharedMesh;
                        continue;
                    }
                }
            }

            List<Font3D.Character3D> characters = Target.characters;
            for (int i = 36; i < characters.Count; i++)
            {
                foreach (Object item in Selection.objects)
                {
                    if (!(item is Mesh || item is GameObject)) continue;
                    string fullName = characters[i].FullName;
                    if (fullName == "") continue;
                    if (!item.name.Contains(fullName)) continue;

                    if (item is Mesh mesh)
                    {
                        fields.ElementAt(i).Value.value = mesh;
                        continue;
                    }

                    if (item is GameObject go && go.TryGetComponent(out MeshFilter filter))
                    {
                        fields.ElementAt(i).Value.value = filter.sharedMesh;
                        continue;
                    }
                }
            }
        }

        private void DrawAlphabetFields()
        {
            Label label = new Label("Alphabet");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(label);
            Foldout foldout = new Foldout
            {
                value = false
            };
            root.Add(foldout);
            if (Target == null) return;
            for (int i = 0; i < Target.characters.Count; i++)
            {
                if (i == 26)
                {
                    EditorGUI.indentLevel--;
                    label = new Label("Numerals");
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    foldout = new Foldout
                    {
                        value = false
                    };
                    root.Add(label);
                    root.Add(foldout);
                }

                if (i == 36)
                {
                    EditorGUI.indentLevel--;
                    label = new Label("Special Characters");
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    foldout = new Foldout
                    {
                        value = false
                    };
                    root.Add(label);
                    root.Add(foldout);
                }

                char c = Target.characters[i].Character;
                ObjectField field = CreateMeshField(i, c);
                foldout.Add(field);
                fields.Add(c, field);
            }
        }

        private ObjectField CreateMeshField(int i, char c)
        {
            ObjectField field = new ObjectField($"{c}  {Target.characters[i].FullName}")
            {
                objectType = typeof(Mesh),
                allowSceneObjects = false,
            };
            field.SetValueWithoutNotify(Target.characters[i].Mesh);
            int index = i;
            field.RegisterValueChangedCallback(o =>
            {
                Target.characters[index].SetMesh((Mesh)o.newValue);
                Debug.Log(Target.characters[index]);
                UnityEditor.EditorUtility.SetDirty(target);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(target);
            });
            return field;
        }
    }
}