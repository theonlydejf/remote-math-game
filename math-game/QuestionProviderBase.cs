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
