namespace RimuruSurvivor;

public static class RimuruAdventureRules
{
    public static bool IsRimuru(string characterName, string surname)
    {
        return string.Equals(characterName?.Trim(), "Rimuru", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(surname?.Trim(), "Tempest", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldAppend(bool alreadyListed, string characterName, string surname)
    {
        return !alreadyListed && IsRimuru(characterName, surname);
    }
}
