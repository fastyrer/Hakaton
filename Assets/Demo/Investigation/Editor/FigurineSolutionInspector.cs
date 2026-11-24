using UnityEditor;
using UnityEngine;
namespace Hakaton.Demo.Investigation.Editor
{
    [CustomEditor(typeof(FigurineSolution))]
    public sealed class FigurineSolutionInspector : UnityEditor.Editor
    {
        static readonly string[] Options={"Не задано","X","Y","Z","K"};
        public override void OnInspectorGUI()
        {
            var solution=(FigurineSolution)target;
            EditorGUILayout.HelpBox("Авторский порядок гнёзд слева направо. ТЗ запрещает придумывать решение: оставьте поля пустыми до согласования.",MessageType.Info);
            if(solution.leftToRight==null||solution.leftToRight.Length!=4)solution.leftToRight=new string[4];
            for(int i=0;i<4;i++)
            {
                int selected=System.Array.IndexOf(Options,solution.leftToRight[i]);int value=EditorGUILayout.Popup("Гнездо "+(i+1),Mathf.Max(0,selected),Options);
                string next=value==0?"":Options[value];if(next!=solution.leftToRight[i]){Undo.RecordObject(solution,"Порядок фигурок");solution.leftToRight[i]=next;EditorUtility.SetDirty(solution);}
            }
            EditorGUILayout.HelpBox(solution.Valid?"Решение готово. Оно будет показано в схеме E01.":"Нужны X, Y, Z, K без повторов. Пока решение не задано, механизм не открывается.",solution.Valid?MessageType.Info:MessageType.Warning);
        }
        [MenuItem("Hakaton/Расследование/Порядок фигурок")]
        static void SelectSolution(){Selection.activeObject=AssetDatabase.LoadAssetAtPath<FigurineSolution>("Assets/Demo/Investigation/Resources/FigurineSolution.asset");}
    }
}
