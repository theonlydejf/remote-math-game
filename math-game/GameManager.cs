
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;

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
        string baseStr = x.ToString(CultureInfo.InvariantCulture);
        // Small chance to add '+' to the start of x if allowed
        if(!difficulty.AllowSigns || x < 0 || rnd.Next(3) != 0)
            return baseStr;
        
        return "+" + baseStr;
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