using UnityEngine.Localization;

public static class LocalizationManager
{
    public static LocalizedString GetLocalizedString(string key)
    {
        var localizedString = new LocalizedString
        {
            TableReference = "UI",
            TableEntryReference = key
        };
        return localizedString;
    }
}