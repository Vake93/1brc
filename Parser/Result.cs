internal struct Result
{
    public double Min;
    public double Max;
    public double Sum;
    public long Count;

    public Result()
    {
        Min = double.MaxValue;
        Max = double.MinValue;
        Sum = 0;
        Count = 0;
    }

    public readonly double Mean => Sum / Count;

    public void Aggregate(double measurement)
    {
        Sum += measurement;
        Count++;
        Min = Math.Min(Min, measurement);
        Max = Math.Max(Max, measurement);
    }

    public void Combine(Result other)
    {
        Sum += other.Sum;
        Count += other.Count;
        Min = Math.Min(Min, other.Min);
        Max = Math.Max(Max, other.Max);
    }

    public override readonly string ToString() => $"{Min:0.0}/{Mean:0.0}/{Max:0.0}";
}
