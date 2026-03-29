namespace UIInfoSuite2.Interfaces;

public interface ISimplePredicate<T>
{
  public bool Matches(T value);

  public bool Matches(in T value)
  {
    return Matches(value);
  }
}
