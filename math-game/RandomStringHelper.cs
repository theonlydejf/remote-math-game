internal static class RandomStringHelper
{
    private static string chars = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM;:";
    private static Random rnd = new();
    public static string GetRandomString(string allowedChars, int length)
    {
        if (string.IsNullOrEmpty(allowedChars))
            throw new ArgumentException("allowedChars cannot be null or empty.", nameof(allowedChars));

        
        return chars[rnd.Next(chars.Length)] + new string(
            Enumerable.Range(0, length)
                      .Select(_ => allowedChars[rnd.Next(allowedChars.Length - 2)])
                      .ToArray()
        ) + chars[rnd.Next(chars.Length)];
    }

    public static string GetVariableFormat(int cnt, double complexityExp)
    {
        int complexity = (int)Math.Pow(cnt, complexityExp);
        int targetSpaceCnt = rnd.Next(complexity / 4, complexity / 2);
        double ratio = rnd.NextDouble();
        string middle1 = new(' ', (int)(targetSpaceCnt * ratio));
        string middle2 = new(' ', (int)(targetSpaceCnt * (1-ratio)));

        return $"{{0}}{middle1}{{1}}{middle2}{{2}}";
    }
}
