using UnityEngine;
namespace Hakaton.Demo.Investigation
{
    [CreateAssetMenu(menuName="Дом помнит/Механизм фигурок")]
    public sealed class FigurineSolution : ScriptableObject
    {
        [Tooltip("Авторский порядок слева направо. Только X, Y, Z, K, без повторов.")]
        public string[] leftToRight=new string[4];
        public bool Valid=>leftToRight!=null&&leftToRight.Length==4&&new System.Collections.Generic.HashSet<string>(leftToRight).SetEquals(new[]{"X","Y","Z","K"});
    }
}
