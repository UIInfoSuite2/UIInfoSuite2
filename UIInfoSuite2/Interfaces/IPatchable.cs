using HarmonyLib;

namespace UIInfoSuite2.Interfaces;

public interface IPatchable
{
  public void Patch(Harmony harmony);
}
