public class MediumQuestionProvider : QuestionProviderBase
{
    private static readonly string[] tests = new string[] { "  {0} {1} {2}", " {0} {1} {2}" };
    public MediumQuestionProvider() : base(HumanInteractionHelper.SimpleQuestions, HumanInteractionHelper.SimpleMessages, tests, HumanInteractionHelper.SIMPLE_ALPHABET)
    {
    }

    protected override string GetEquationFormat() => " {0} {1} {2}";
}
