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
