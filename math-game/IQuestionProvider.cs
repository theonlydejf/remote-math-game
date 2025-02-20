public interface IQuestionProvider
{
    string GetQuestion(string numberA, string numberB, string op);
    string GetMessage();
    public int Count { get; }
}
