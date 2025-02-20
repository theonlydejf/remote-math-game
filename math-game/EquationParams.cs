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
