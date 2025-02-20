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
