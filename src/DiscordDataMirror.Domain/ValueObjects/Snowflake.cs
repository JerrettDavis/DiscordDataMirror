namespace DiscordDataMirror.Domain.ValueObjects;

/// <summary>
/// Discord Snowflake ID wrapper. Stores as string for PostgreSQL compatibility.
/// </summary>
public readonly record struct Snowflake : IComparable<Snowflake>
{
    public string Value { get; }
    
    private static readonly DateTime DiscordEpoch = new(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    
    public Snowflake(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Snowflake cannot be empty", nameof(value));
        if (!ulong.TryParse(value, out _))
            throw new ArgumentException("Snowflake must be a valid numeric string", nameof(value));
        Value = value;
    }
    
    public Snowflake(ulong value) => Value = value.ToString();
    
    public ulong ToUInt64() => ulong.Parse(Value);
    
    /// <summary>
    /// Extracts the timestamp from the Snowflake.
    /// </summary>
    public DateTime Timestamp
    {
        get
        {
            var snowflake = ToUInt64();
            var milliseconds = (long)(snowflake >> 22);
            return DiscordEpoch.AddMilliseconds(milliseconds);
        }
    }
    
    public static implicit operator string(Snowflake snowflake) => snowflake.Value;
    
    // Removed implicit string→Snowflake to prevent accidental empty string conversions
    // Use new Snowflake(value) or Snowflake.TryParse() instead
    
    public static implicit operator Snowflake(ulong value) => new(value);
    
    public override string ToString() => Value;
    
    public int CompareTo(Snowflake other) => ToUInt64().CompareTo(other.ToUInt64());
    
    public static Snowflake Empty => new("0");
    
    /// <summary>
    /// Attempts to parse a string as a Snowflake.
    /// </summary>
    public static bool TryParse(string? value, out Snowflake snowflake)
    {
        if (string.IsNullOrWhiteSpace(value) || !ulong.TryParse(value, out _))
        {
            snowflake = Empty;
            return false;
        }
        
        snowflake = new Snowflake(value);
        return true;
    }
}
