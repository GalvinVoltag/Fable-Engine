namespace FableEngine.Math;

public record struct Vector : IVector
{
    private readonly float[] digits;
    public Vector(int length)
    {
        digits = new float[length];
        Dimensions = length;
    }

    public Vector(float[] digits)
    {
        this.digits = digits;
        Dimensions = digits.Length;
    }
    public float this[int index]
    {
        get => digits[index];
        set => digits[index] = value;
    }

    public IVector New() => new Vector(digits);
    public override string ToString()
    {
        string r = "<";
        for (int i = 0; i < Dimensions; i++)
            r += this[i] + ", ";
        return r + ">";
    }


    public int Dimensions { get; private set; }
}