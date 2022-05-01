//using System;

//public enum Language { Arabic, English }

//public class LanguageManager
//{
//    //private static LanguageManager i;
//    //public static LanguageManager I => i ??= new LanguageManager();

//    private static Language currentLanguage = Language.Arabic;
//    public static Language CurrentLanguage
//    {
//        get
//        {
//            return currentLanguage;
//        }
//        set
//        {
//            currentLanguage = value;
//            RefreshLanguageForActiveTranslatables();
//        }
//    }

//    private static Language[] languagesEnumValues = (Language[])Enum.GetValues(typeof(Language));
//    public static Language[] LanguagesEnumValues => languagesEnumValues;

//    private static void RefreshLanguageForActiveTranslatables()
//    {
//        Object.FindObjectsOfType<Translatable>().ForEach(t => t.IsTranslatable);
//    }

//}
