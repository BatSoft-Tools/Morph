namespace Morph.Params
{
  public abstract class ValueObject : Value
  {
    public ValueObject(string TypeName)
    {
      fTypeName = TypeName;
    }

    private string fTypeName;
    public string TypeName
    {
      get { return fTypeName; }
    }
  }
}