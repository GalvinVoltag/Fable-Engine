using FableEngine.Core;

public struct Ref<T>(T? value = null) : IEquatable<Ref<T>> where T : class
{
    public T? value
    {
        get
        {
            if (field is IDestroyable { destroyed: true }) field = null;
            return field;
        }
        set;
    } = value;

    public static bool operator ==(Ref<T> Ref, object o) => Ref.value == o;
    public static bool operator !=(Ref<T> Ref, object o) => Ref.value != o;
    public bool Equals(Ref<T> other) => other == this;
    public override bool Equals(object? obj) => obj == value;
    public override int GetHashCode() => value != null ? EqualityComparer<T?>.Default.GetHashCode(value) : 0;
    public static implicit operator T?(Ref<T> Ref) => Ref.value;
    public static implicit operator Ref<T>(T? t) => new Ref<T>(t);
}