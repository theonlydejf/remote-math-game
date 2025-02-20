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
