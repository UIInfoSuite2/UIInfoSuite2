namespace UIInfoSuite2.Interfaces;

internal interface ITrackable
{
  public bool IsDirty { get; }
  public void ResetDirty();
}
