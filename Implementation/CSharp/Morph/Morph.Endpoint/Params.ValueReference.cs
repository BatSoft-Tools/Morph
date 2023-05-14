namespace Morph.Params
{
  public class ValueReference : ValueObject
  {
    public ValueReference(string TypeName, int ID)
      : base(TypeName)
    {
      fID = ID;
    }

    private int fID;
    public int ID
    {
      get { return fID; }
    }
  }
}