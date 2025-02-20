public enum ParserDifficulty { EASY, MEDIUM, HARD, ADVANCED }

public struct Difficulty
{
    private static Dictionary<ParserDifficulty, double> parser2expMap = new()
    {
        { ParserDifficulty.EASY, 1 },
        { ParserDifficulty.MEDIUM, 1 },
        { ParserDifficulty.HARD, 1 },
        { ParserDifficulty.ADVANCED, 1 }
    };
    private static Dictionary<ParserDifficulty, double> parser2mulMap = new()
    {
        { ParserDifficulty.EASY, 1 },
        { ParserDifficulty.MEDIUM, 3 },
        { ParserDifficulty.HARD, 6 },
        { ParserDifficulty.ADVANCED, 12 }
    };

    private const double SIGNS_SCORE_MUL = 2;
    private const double MSG_SCORE_MUL = 3;

    public double ScoreExponent { get; } = 1;
    public double ScoreMultiplier { get; } = 1;

    public bool AllowSigns { get; }
    public bool AllowMessages { get; }
    public ParserDifficulty ParserDifficulty { get; }

    public Difficulty(bool allowSigns, bool allowMessages, ParserDifficulty parserDifficulty)
    {
        AllowSigns = allowSigns;
        AllowMessages = allowMessages;
        ParserDifficulty = parserDifficulty;

        ScoreExponent = parser2expMap[parserDifficulty];
        ScoreMultiplier *= parser2mulMap[parserDifficulty];
        if(AllowSigns)
            ScoreExponent *= SIGNS_SCORE_MUL;
        if(AllowMessages)
            ScoreExponent *= MSG_SCORE_MUL;
    }

    public static bool TryParse(string? str, out Difficulty? difficulty)
    {
        difficulty = null;
        if(str == null)
            return false;

        string[] modeIDs = str.ToLower().Split(';');
        if(modeIDs.Length != 3)
            return false;

        bool allowSigns = modeIDs[0].StartsWith('t');
        bool allowMessages = modeIDs[1].StartsWith('t');
        bool validParserDifficulty = Enum.TryParse(modeIDs[2].ToUpper(), out ParserDifficulty parserDifficulty);
        if(!validParserDifficulty)
            return false;
        
        difficulty = new(allowSigns, allowMessages, parserDifficulty);
        return true;
    }

    public override string ToString()
    {
        return $"{AllowSigns};{AllowMessages};{ParserDifficulty}";
    }
}
