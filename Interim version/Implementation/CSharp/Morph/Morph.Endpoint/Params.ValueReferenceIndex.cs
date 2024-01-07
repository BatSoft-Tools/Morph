namespace Morph.Params
{
  public class ValueReferenceIndex : ValueReference
  {
    public ValueReferenceIndex(string TypeName, int ID, Value Index)
      : base(TypeName, ID)
    {
      fIndex = Index;
    }

    private Value fIndex;
    public Value Index
    {
      get { return fIndex; }
    }
  }
}