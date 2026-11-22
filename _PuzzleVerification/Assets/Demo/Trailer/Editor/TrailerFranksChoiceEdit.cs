using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Hakaton.Demo.Trailer.Editor
{
    public static class TrailerFranksChoiceEdit
    {
        const string PlanPath="Assets/Demo/Trailer/Art/FranksChoiceEdit.json";
        const string TemplatePath="Assets/Demo/Trailer/Art/TrailerBaseShots.json";
        [Serializable] sealed class Plan {public string music;public float[] cuts;}
        [Serializable] sealed class Templates {public TrailerShot[] shots;}
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        [MenuItem("Hakaton/Трейлер/Полный монтаж под Frank's Choice")]
        public static void ApplyCurrent()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="OpeningTrailer")throw new InvalidOperationException("Откройте OpeningTrailer и остановите Play.");
            Apply(UnityEngine.Object.FindAnyObjectByType<OpeningTrailer>());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply(OpeningTrailer director)
        {
            if(director==null)throw new ArgumentNullException(nameof(director));
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(plan.music);
            if(clip==null)throw new InvalidOperationException("Не импортирован трек: "+plan.music);
            if(!File.Exists(TemplatePath))
            {
                if(director.shots.Length!=14)throw new InvalidOperationException("Для первого монтажа нужны исходные 14 планов.");
                File.WriteAllText(TemplatePath,JsonUtility.ToJson(new Templates{shots=director.shots},true));AssetDatabase.ImportAsset(TemplatePath);
            }
            var source=JsonUtility.FromJson<Templates>(File.ReadAllText(TemplatePath)).shots;
            var edit=new List<TrailerShot>();
            Action<int,string,string,string> add=(index,label,name,role)=>
            {
                var shot=JsonUtility.FromJson<TrailerShot>(JsonUtility.ToJson(source[index]));
                shot.label=label;shot.name=name;shot.role=role;edit.Add(shot);
            };
            add(0,"НЬЮ-ЙОРК / 2000","ПОЛИЦЕЙСКИЙ УЧАСТОК","НОЧНАЯ СМЕНА");
            add(0,"01 / ПРИБЫТИЕ","","");
            edit[1].from=V(8,2.1f,-13);edit[1].to=V(2.5f,1.7f,-7);edit[1].lookFrom=V(0,1.6f,0);edit[1].lookTo=V(0,1.6f,1);
            add(1,"02 / ДЕЖУРНАЯ ЧАСТЬ","","");
            add(1,"03 / ОПЕРАТИВНЫЙ ЗАЛ","","СОТРУДНИКИ НОЧНОЙ СМЕНЫ");
            edit[3].from=V(5,1.6f,9.3f);edit[3].to=V(10,1.65f,9.3f);edit[3].lookFrom=V(9,1.25f,12);edit[3].lookTo=V(13,1.25f,12);edit[3].fieldOfView=54;
            add(2,"04 / ДЕТЕКТИВНЫЙ ОТДЕЛ","ДЕТЕКТИВ","НАПАРНИК АЛЕКСЕЯ · ПОДОЗРЕВАЕТ ЕГО");
            add(12,"05 / ДЕТЕКТИВНЫЙ ОТДЕЛ","","");
            add(3,"06 / КОНВОЙ","ЗАДЕРЖАННЫЙ","КОНВОИР ВЕДЁТ ЕГО В ИЗОЛЯТОР");
            add(3,"07 / ИЗОЛЯТОР","","");
            edit[7].from=V(-8.8f,1.7f,28);edit[7].to=V(-8.8f,1.7f,31.2f);edit[7].lookFrom=V(-2,1.15f,30);edit[7].lookTo=V(-4,1.15f,30);edit[7].fieldOfView=50;
            add(4,"08 / СКРЫТОЕ НАБЛЮДЕНИЕ","","ЧЕЛОВЕК В ПАЛЬТО И ОЧКАХ");
            add(4,"09 / ЧУЖОЙ ВЗГЛЯД","","");
            edit[9].from=V(61.6f,1.78f,8.95f);edit[9].to=V(62.3f,1.78f,9.25f);edit[9].fieldOfView=29;
            add(5,"10 / КОМНАТА ПРОСЛУШКИ","ЗАПИСЬ ПРОДОЛЖАЕТСЯ","АГЕНТЫ ЗА КАТУШЕЧНЫМИ МАГНИТОФОНАМИ");
            add(5,"11 / МАГНИТНАЯ ЛЕНТА","","КАНАЛ 04 · ЗАПИСЬ");
            edit[11].from=V(85.8f,1.45f,3.1f);edit[11].to=V(86.8f,1.5f,3.3f);edit[11].lookFrom=edit[11].lookTo=V(86.3f,1.4f,4.8f);edit[11].fieldOfView=42;
            add(5,"12 / ОПЕРАТОР ПРОСЛУШКИ","","В НАУШНИКАХ — ЧУЖОЙ РАЗГОВОР");
            edit[12].from=V(92,1.7f,3.6f);edit[12].to=V(91.5f,1.65f,4.1f);edit[12].lookFrom=edit[12].lookTo=V(90.9f,1.49f,6.1f);edit[12].fieldOfView=40;
            add(6,"13 / ДЕЙСТВУЮЩИЕ ЛИЦА","АЛЕКСЕЙ","ГЛАВНЫЙ ГЕРОЙ · РАССЛЕДУЕТ СМЕРТЬ ОТЦА");
            add(6,"14 / ЛИЧНОЕ ДЕЛО","","ОН ВЗЯЛ ОТПУСК РАДИ ЭТОГО РАССЛЕДОВАНИЯ");
            edit[14].from=V(120.9f,1.55f,1.3f);edit[14].to=V(120.1f,1.6f,1.7f);edit[14].fieldOfView=36;
            add(7,"15 / ДЕЙСТВУЮЩИЕ ЛИЦА","ИВАН","БРАТ АЛЕКСЕЯ · РАЦИОНАЛЬНЫЙ МЫСЛИТЕЛЬ");
            add(7,"16 / ИВАН","","");
            edit[16].from=V(139.6f,1.45f,1.4f);edit[16].to=V(140.4f,1.4f,1.5f);edit[16].lookFrom=edit[16].lookTo=V(140,1.2f,2.75f);edit[16].fieldOfView=40;
            add(8,"17 / ДЕЙСТВУЮЩИЕ ЛИЦА","ДМИТРИЙ","БРАТ АЛЕКСЕЯ · ВСПЫЛЬЧИВЫЙ, ЖИВЁТ ЭМОЦИЯМИ");
            add(8,"18 / ДМИТРИЙ","","");
            edit[18].from=V(160.8f,1.3f,.9f);edit[18].to=V(160.3f,1.4f,1.2f);edit[18].lookFrom=V(160,1.1f,2.8f);edit[18].lookTo=V(160,1.4f,3);edit[18].fieldOfView=40;
            add(9,"19 / ДЕЙСТВУЮЩИЕ ЛИЦА","ПАВЕЛ","ЧЕЛОВЕК, КОТОРОМУ НЕЛЬЗЯ ДОВЕРЯТЬ");
            add(9,"20 / ПАВЕЛ","","");
            edit[20].from=V(181,1.7f,1.25f);edit[20].to=V(180.6f,1.7f,1.5f);edit[20].lookFrom=edit[20].lookTo=V(180,1.6f,3);edit[20].fieldOfView=32;
            add(10,"21 / СЕМЕЙНЫЙ АРХИВ","ФЁДОР","ОТЕЦ · ПОГИБ ДО НАЧАЛА ЭТИХ СОБЫТИЙ");
            add(11,"22 / ПОЛИЦЕЙСКИЙ АРХИВ","","ДЕЛО В-97/041");
            edit[22].from=V(6,1.7f,30.5f);edit[22].to=V(8,1.7f,31.8f);edit[22].lookFrom=V(12,1.5f,35.8f);edit[22].lookTo=V(14,1.5f,35.8f);edit[22].fieldOfView=54;
            add(11,"23 / ПРОТОКОЛ","ПРОТОКОЛ ОБНАРУЖЕНИЯ ТЕЛА","ПИСТОЛЕТ ЛЕЖАЛ РЯДОМ С ОТЦОМ");
            edit[23].from=V(6.5f,1.6f,25.7f);edit[23].to=V(7.6f,1.5f,25.8f);edit[23].lookFrom=edit[23].lookTo=V(7,.95f,27);edit[23].fieldOfView=40;
            add(11,"24 / ВЕЩЕСТВЕННОЕ ДОКАЗАТЕЛЬСТВО","COLT M1911A1","ПИСТОЛЕТ · КАЛИБР .45 ACP");
            edit[24].from=V(9.3f,1.55f,25.9f);edit[24].to=V(10.4f,1.4f,26);edit[24].lookFrom=edit[24].lookTo=V(10,1,27.3f);edit[24].fieldOfView=40;
            add(11,"25 / ЗАКЛЮЧЕНИЕ ЭКСПЕРТА","ДРУГОЙ КАЛИБР","ПУЛЯ 9×19 ММ · ОРУЖИЕ НЕ СОВПАДАЕТ");
            edit[25].from=V(12.3f,1.5f,25.7f);edit[25].to=V(13.6f,1.45f,25.85f);edit[25].lookFrom=edit[25].lookTo=V(13,.95f,27);edit[25].fieldOfView=40;
            add(4,"26 / НАБЛЮДЕНИЕ","","");
            edit[26].from=V(62.6f,1.78f,9.1f);edit[26].to=V(61.8f,1.78f,9.2f);edit[26].fieldOfView=32;
            add(12,"27 / НАПАРНИК","","");
            add(1,"28 / НОЧНАЯ СМЕНА","","ДЕЛО ОТЦА ЕЩЁ НЕ ЗАКРЫТО");
            edit[28].from=V(0,1.55f,10);edit[28].to=V(0,1.55f,16);edit[28].lookFrom=edit[28].lookTo=V(0,1.6f,39);edit[28].fieldOfView=49;
            add(13,"TYLER BATES / FRANK'S CHOICE",""," ");
            if(plan.cuts.Length!=edit.Count+1)throw new InvalidOperationException("Число склеек не совпадает с монтажом.");
            for(int i=0;i<edit.Count;i++){edit[i].exterior=i<2;edit[i].start=plan.cuts[i];edit[i].duration=(i==edit.Count-1?clip.length:plan.cuts[i+1])-plan.cuts[i];}
            Undo.RecordObject(director,"Монтаж полного трека");director.shots=edit.ToArray();
            Undo.RecordObject(director.music,"Frank's Choice");director.music.clip=clip;director.music.name="Tyler Bates — Frank's Choice · полный трек";
            director.musicFadeIn=0;director.musicFadeOut=0;
            foreach(var a in director.actors.Where(a=>a.walking))
            {Undo.RecordObject(a,"Время конвоя");a.walkStart=edit[6].start;a.walkDuration=edit[7].start+edit[7].duration-edit[6].start;}
            var importer=(AudioImporter)AssetImporter.GetAtPath(plan.music);var settings=importer.defaultSampleSettings;
            if(settings.loadType!=AudioClipLoadType.Streaming){settings.loadType=AudioClipLoadType.Streaming;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
            EditorUtility.SetDirty(director);EditorUtility.SetDirty(director.music);EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        }
    }
}
