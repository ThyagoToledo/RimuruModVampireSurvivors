namespace System.Runtime.CompilerServices;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Event |
    AttributeTargets.Field |
    AttributeTargets.GenericParameter |
    AttributeTargets.Module |
    AttributeTargets.Parameter |
    AttributeTargets.Property |
    AttributeTargets.ReturnValue |
    AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false)]
internal sealed class NullableAttribute : Attribute
{
    public NullableAttribute(byte flag)
    {
        NullableFlags = new[] { flag };
    }

    public NullableAttribute(byte[] flags)
    {
        NullableFlags = flags;
    }

    public byte[] NullableFlags { get; }
}

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Delegate |
    AttributeTargets.Interface |
    AttributeTargets.Method |
    AttributeTargets.Module |
    AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false)]
internal sealed class NullableContextAttribute : Attribute
{
    public NullableContextAttribute(byte flag)
    {
        Flag = flag;
    }

    public byte Flag { get; }
}
