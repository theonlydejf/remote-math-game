
using System.Reflection.Metadata;
using System.Text;

internal static class RandomStringHelper
{
    private static Random rnd = new();
    public static string GetRandomString(string allowedChars, int length)
    {
        if (string.IsNullOrEmpty(allowedChars))
            throw new ArgumentException("allowedChars cannot be null or empty.", nameof(allowedChars));

        
        return new string(
            Enumerable.Range(0, length)
                      .Select(_ => allowedChars[rnd.Next(allowedChars.Length)])
                      .ToArray()
        );
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

internal static class HumanInteractionHelper
{
    public const int HUMAN_QUESTION_CNT = 10;
    public const string SIMPLE_ALPHABET = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm;: ";
    public const string ADVANCED_ALPHABET = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm;: 1234567890";

    public static readonly string[] SimpleQuestions = new string[]
    {
        "Kolik je{0}?",
        "Vypocitej{0}",
        "Vypocitej{0}!",
        "Pomoz mi spocitat{0} prosim.",
        "Tvuj priklad je:{0}",
        "Tak schvalne, jde ti matika? Jestli jo tak priklad{0} by ti nemel delat problem :D"
    };

    public static readonly string[] HardQuestions = SimpleQuestions.Concat(new string[] { "{0}" }).ToArray();
    public static readonly string[] AdvancedQuestions = HardQuestions.Concat(new string[]
    {
        "Jedna z mych 1000 otazek je kolik vyjde:{0}?",
        "Moje oblibena cisla jsou 10, 15, 34 a vysledek{0}, jake je me oblibene cislo na 4 pozici?",
        "Pokud vysledek prikladu{0} vynasobim podilem cisla 10 a rozdilu cisel 15 a 5, vyjde mi kolik?"
    }).ToArray();

    public static readonly string[] SimpleMessages = new string[]
    {
        "Priprav se!",
        "Jsi vybornej!",
        "Jde ti to krasne!",
        "Pospes si, jinak nevyhrajes",
        "Super prace!",
        ""
    };

    public static readonly string[] AdvancedMessages = SimpleMessages.Concat(new string[]
    {
        "Jen tak dal a budes v top 10!",
        "K dalsimu vysledku pricti 100 a pak odecti dvojnasobek dvojnasobek cisla 50"
    }).ToArray();

    private static Random rnd = new Random();
}

public struct EquationParams
{
    public string NumberA { get; set; }
    public string NumberB { get; set; }
    public double Result { get; set; }
    public string Operator { get; set; }

    public EquationParams(string a, string b, double result, string op)
    {
        NumberA = a;
        NumberB = b;
        Result = result;
        Operator = op;
    }
}

public interface IQuestionProvider
{
    string GetQuestion(string numberA, string numberB, string op);
    string GetMessage();
    public int Count { get; }
}

public class EasyQuestionProvider : IQuestionProvider
{
    private static Random rnd = new();

    public int Count { get; private set; } = 0;
    public string GetMessage()
    {
        if(Count <= HumanInteractionHelper.HUMAN_QUESTION_CNT)
            return HumanInteractionHelper.SimpleMessages[rnd.Next(HumanInteractionHelper.SimpleMessages.Length)];
        
        return RandomStringHelper.GetRandomString(HumanInteractionHelper.SIMPLE_ALPHABET, rnd.Next(99) + 1);
    }

    public string GetQuestion(string numberA, string numberB, string op)
    {
        Count++;
        return $"{numberA} {op} {numberB}";
    }
}

public abstract class QuestionProviderBase : IQuestionProvider
{
    public const double COMPLEXITY_EXP_MIN = 1.3;
    public const double COMPLEXITY_EXP_MAX = 2;

    public int Count { get; private set; } = 0;
    protected readonly Random rnd = new();

    private readonly string[] questions;
    private readonly string[] messages;
    private readonly string[]? tests;
    private readonly string alphabet;

    public QuestionProviderBase(string[] questions, string[] messages, string[]? tests, string alphabet)
    {
        this.questions = questions;
        this.messages = messages;
        this.tests = tests;
        this.alphabet = alphabet;
    }

    public string GetQuestion(string numberA, string numberB, string op)
    {
        Count++;
        string format;
        if(tests != null && Count > HumanInteractionHelper.HUMAN_QUESTION_CNT && Count <= HumanInteractionHelper.HUMAN_QUESTION_CNT + tests.Length)
            format = tests[Count - HumanInteractionHelper.HUMAN_QUESTION_CNT - 1];
        else
            format = GetFormat();
        return string.Format(format, numberA, op, numberB);
    }

    private string GetFormat()
    {
        if(Count <= HumanInteractionHelper.HUMAN_QUESTION_CNT)
            return string.Format(questions[rnd.Next(questions.Length)], GetEquationFormat());
        
        int targetLen = rnd.Next((int)Math.Pow(Count, COMPLEXITY_EXP_MIN), (int)Math.Pow(Count, COMPLEXITY_EXP_MAX));
        switch(rnd.Next(3))
        {
            case 0:
                return RandomStringHelper.GetRandomString(alphabet, targetLen) + GetEquationFormat();
            case 1:
                double ratio = rnd.NextDouble();
                return RandomStringHelper.GetRandomString(alphabet, (int)(targetLen * ratio)) + GetEquationFormat()
                    + RandomStringHelper.GetRandomString(alphabet, (int)(targetLen * (1-ratio)));
            case 2:
                return GetEquationFormat() + RandomStringHelper.GetRandomString(alphabet, targetLen);
        }
        return "Muj program je debilni" + GetEquationFormat();
    }

    protected abstract string GetEquationFormat();

    public string GetMessage()
    {
        if(Count <= HumanInteractionHelper.HUMAN_QUESTION_CNT)
            return messages[rnd.Next(messages.Length)];
        
        return RandomStringHelper.GetRandomString(alphabet, rnd.Next(99) + 1);
    }
}

public class MediumQuestionProvider : QuestionProviderBase
{
    private static readonly string[] tests = new string[] { "  {0} {1} {2}", " {0} {1} {2}" };
    public MediumQuestionProvider() : base(HumanInteractionHelper.SimpleQuestions, HumanInteractionHelper.SimpleMessages, tests, HumanInteractionHelper.SIMPLE_ALPHABET)
    {
    }

    protected override string GetEquationFormat() => " {0} {1} {2}";
}

public class HardQuestionProvider : QuestionProviderBase
{
    private static readonly string[] tests = new string[]
    {
        "{0} {1} {2}",
        "{0}{1}{2}",
        " {0}{1}{2}",
        "{0}{1}{2} ",
        "{0}{1}{2}",
        "!{0}{1}{2}!",
        "{0}{1}{2}!",
        "!{0}{1}{2}!",
        "{0}       {1}      {2}",
        "   {0}{1}{2}",
        "{0}{1} {2}",
        "{0} {1}{2}"
    };
    public HardQuestionProvider() : base(HumanInteractionHelper.HardQuestions, HumanInteractionHelper.SimpleMessages, tests, HumanInteractionHelper.SIMPLE_ALPHABET)
    {
    }

    protected override string GetEquationFormat() 
    {
        string prefix = new(' ', rnd.Next(3));
        string postfix = new(' ', rnd.Next(3));
        string format;
        if(Count <= HumanInteractionHelper.HUMAN_QUESTION_CNT)
        {
            string middle1 = new(' ', rnd.Next(4));
            string middle2 = new(' ', rnd.Next(4));
            format = $"{prefix}{{0}}{middle1}{{1}}{middle2}{{2}}{postfix}";
        }
        else
            format = RandomStringHelper.GetVariableFormat(Count, COMPLEXITY_EXP_MIN / 1.5);

        return $"{prefix}{format}{postfix}";
    }
}

public class AdvancedQuestionProvider : QuestionProviderBase
{
    private static readonly string[] tests = new string[]
    {
        "{0} {1} {2}",
        "{0}{1}{2}",
        " {0}{1}{2}",
        "{0}{1}{2} ",
        "{0}{1}{2}",
        "!{0}{1}{2}!",
        "{0}{1}{2}!",
        "!{0}{1}{2}!",
        "{0}       {1}      {2}",
        "   {0}{1}{2}",
        "{0}{1} {2}",
        "{0} {1}{2}",
        "123 {0}{1}{2} 123",
        "123    {0}{1}   {2} 123",
        "123 {0}       {1}{2}     123"
    };
    public AdvancedQuestionProvider() : base(HumanInteractionHelper.AdvancedQuestions, HumanInteractionHelper.AdvancedQuestions, tests, HumanInteractionHelper.ADVANCED_ALPHABET)
    {
    }

    protected override string GetEquationFormat()
    {
        string prefix = new(' ', rnd.Next(3));
        string postfix = new(' ', rnd.Next(3));
        string format;
        if(Count <= HumanInteractionHelper.HUMAN_QUESTION_CNT)
        {
            string middle1 = new(' ', rnd.Next(4));
            string middle2 = new(' ', rnd.Next(4));
            format = $"{prefix}{{0}}{middle1}{{1}}{middle2}{{2}}{postfix}";
        }
        else
            format = RandomStringHelper.GetVariableFormat(Count, COMPLEXITY_EXP_MIN / 1.5);

        return $"{prefix}{format}{postfix}";
    }
}

public enum ParserDifficulty { EASY, MEDIUM, HARD, ADVANCED }

public struct Difficulty
{
    private static Dictionary<ParserDifficulty, double> parser2expMap = new()
    {
        { ParserDifficulty.EASY, 1 },
        { ParserDifficulty.MEDIUM, 1.5 },
        { ParserDifficulty.HARD, 2 },
        { ParserDifficulty.ADVANCED, 3 }
    };

    private const double SIGNS_SCORE_MUL = 2;
    private const double MSG_SCORE_MUL = 3;

    public double ScoreExponent { get; }
    public double ScoreMultiplier { get; }

    public bool AllowSigns { get; }
    public bool AllowMessages { get; }
    public ParserDifficulty ParserDifficulty { get; }

    public Difficulty(bool allowSigns, bool allowMessages, ParserDifficulty parserDifficulty)
    {
        AllowSigns = allowSigns;
        AllowMessages = allowMessages;
        ParserDifficulty = parserDifficulty;

        ScoreExponent = parser2expMap[parserDifficulty];
        ScoreMultiplier = 1;
        if(AllowSigns)
            ScoreExponent *= SIGNS_SCORE_MUL;
        if(AllowMessages)
            ScoreExponent *= MSG_SCORE_MUL;
    }

    public static bool TryParse(string str, out Difficulty? difficulty)
    {
        difficulty = null;
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

public class GameManager
{
    private Random rnd = new();
    private Difficulty difficulty;
    private IQuestionProvider? questionProvider;
    private bool first = true;

    public GameManager(Difficulty difficulty) 
    {
        this.difficulty = difficulty;
        questionProvider = this.difficulty.ParserDifficulty switch
        {
            ParserDifficulty.EASY => new EasyQuestionProvider(),
            ParserDifficulty.MEDIUM => new MediumQuestionProvider(),
            ParserDifficulty.HARD => new HardQuestionProvider(),
            ParserDifficulty.ADVANCED => new AdvancedQuestionProvider(),
            _ => throw new FormatException("Bad difficulty")
        };
    }

    private string num2str(double x)
    {
        // Small chance to add '+' to the start of x if allowed
        if(!difficulty.AllowSigns || x < 0 || rnd.Next(3) != 0)
            return x.ToString();
        
        return "+" + x.ToString();
    }

    private EquationParams GetSimpleEquationParams()
    {
        int minMul = difficulty.AllowSigns ? -1 : 0;
        double a = rnd.Next(100 * minMul, 100);
        double b = rnd.Next(100 * minMul, 100);

        switch (rnd.Next(4))
        {
            case 0: // Add
                return new(num2str(a), num2str(b), a+b, "+");
            case 1: // Sub
                return new(num2str(a), num2str(b), a-b, "-");
            case 2: // Mul
                a = rnd.Next(20 * minMul, 21);
                b = rnd.Next(20 * minMul, 21);
                return new(num2str(a), num2str(b), a*b, "*");
            case 3: // Div
                a = rnd.Next(10 * minMul, 11);
                if(rnd.Next(2) == 0)
                    b = rnd.Next(1, 11);
                else
                    b = rnd.Next(-10, 0);
                return new(num2str(a * b), num2str(b), a, "/");
            default:
                throw new InvalidOperationException("Unexpected operation type");
        }
    }
    private EquationParams GetComputerEquationParams()
    {
        const int MAX_VALUE = 99999999;

        double a = rnd.NextDouble() * MAX_VALUE;
        double b = rnd.NextDouble() * MAX_VALUE;
        if(difficulty.AllowSigns)
        {
            a -= MAX_VALUE / 2;
            b -= MAX_VALUE / 2;
        }

        return rnd.Next(4) switch
        {
            0 => new(num2str(a), num2str(b), a + b, "+"),
            1 => new(num2str(a), num2str(b), a - b, "-"),
            2 => new(num2str(a), num2str(b), a * b, "*"),
            3 => new(num2str(a), num2str(b), a / b, "/"),
            _ => throw new InvalidOperationException("Unexpected operation type"),
        };
    }

    public async Task<bool> PlayOnce(StreamWriter sw, StreamReader sr, CancellationToken ct)
    {
        if(questionProvider == null)
            throw new ArgumentException("No question provider set");

        // Try show message
        if(difficulty.AllowMessages && !first && rnd.Next(4) == 2)
        {
            await sw.WriteLineAsync(questionProvider.GetMessage());
            await sw.FlushAsync();
        }
        first = false;

        // Create equation values
        EquationParams eqParams;
        if(questionProvider.Count > HumanInteractionHelper.HUMAN_QUESTION_CNT)
            eqParams = GetComputerEquationParams();
        else
            eqParams = GetSimpleEquationParams();

        // Send question
        await sw.WriteLineAsync(questionProvider.GetQuestion(eqParams.NumberA, eqParams.NumberB, eqParams.Operator));
        await sw.FlushAsync();

        // Get answer
        ValueTask<string?> readValueTask = sr.ReadLineAsync(ct);
        Task<string?> readTask = readValueTask.AsTask();
        Task completedTask = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, ct));
        if(completedTask != readTask) // Make sure to cancel immidiately when requested
            throw new OperationCanceledException();
        
        // Check answer corectness
        string? answerStr = await readValueTask;
        if (answerStr == null || !double.TryParse(answerStr, out double answerDbl))
            return false;

        return Math.Abs(answerDbl - eqParams.Result) < 1e-9;
    }
}