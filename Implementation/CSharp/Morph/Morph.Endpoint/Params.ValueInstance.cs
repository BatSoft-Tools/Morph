using System.Collections.Generic;

namespace Morph.Params
{
  public class ValueInstance : ValueObject
  {
    public ValueInstance(string TypeName, bool IsStruct, bool IsArray)
      : base(TypeName)
    {
      if (IsArray)
        fArray = new ArrayValues();
      if (IsStruct)
        fStruct = new StructValues();
    }

    private ArrayValues fArray;
    public ArrayValues Array
    {
      get { return fArray; }
    }

    private StructValues fStruct;
    public StructValues Struct
    {
      get { return fStruct; }
    }
  }

  public class ArrayValues
  {
    private Values fValues = new Values();

    public int Count
    {
      get { return fValues.Count; }
    }

    public Values Values
    {
      get { return fValues; }
    }

    public void Add(object Value)
    {
      fValues.Add(Value);
    }
  }

  public class StructValues
  {
    private Values fValues = new Values();
    private Names fNames = new Names();

    public int Count
    {
      get { return fValues.Count; }
    }

    public Values Values
    {
      get { return fValues; }
    }

    public Names Names
    {
      get { return fNames; }
    }

    public object ByName(string Name)
    {
      int i = fNames.IndexOf(Name);
      if (i < 0)
        throw new EMorph("Identifier not found: " + Name);
      return fValues[i];
    }

    public void Add(object Value, string Name)
    {
      fValues.Add(Value);
      fNames.Add(Name);
    }

    public object[] ToObjects()
    {
      if (Count == 0)
        return null;
      return fValues.fValues.ToArray();
    }
  }

  public class Values
  {
    internal List<object> fValues = new List<object>();

    internal int Count
    {
      get { return fValues.Count; }
    }

    public object this[int i]
    {
      get { return fValues[i]; }
    }

    internal void Add(object Value)
    {
      fValues.Add(Value);
    }

    internal object[] ToArray()
    {
      return fValues.ToArray();
    }
  }

  public class Names
  {
    private List<string> fNames = new List<string>();

    internal int Count
    {
      get { return fNames.Count; }
    }

    public string this[int i]
    {
      get { return fNames[i]; }
    }

    internal int IndexOf(string Name)
    {
      return fNames.IndexOf(Name);
    }

    internal void Add(string Name)
    {
      fNames.Add(Name);
    }
  }
}