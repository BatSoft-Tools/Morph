namespace Morph.Lib
{
  /**
   * As a Morph rule, any ID=0 is the same as saying ID=No_ID.
   * Also, some platforms might not deal with unsigned integers.
   * Therefore, valid ID's range from {1..0x7FFFFFFF}.
   **/
  public interface IIDFactory
  {
    int Generate();
    void Release(int id);
  }

  public class IDSeed : IIDFactory
  {
    public IDSeed()
    {
      fSeed = 1;
    }

    public IDSeed(int StartID)
    {
      fSeed = StartID;
    }

    private int fSeed;

    #region IIDFactory Members

    public int Generate()
    {
      lock (this)
      {
        if (fSeed == int.MaxValue)
          fSeed = 1;
        return fSeed++;
      }
    }

    public void Release(int id)
    {
    }

    #endregion
  }
}