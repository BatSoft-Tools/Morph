using System.Reflection;
using Morph.Lib;

namespace Morph.Endpoint
{
  public class LinkProperty : LinkMember
  {
    public LinkProperty(string Name, bool IsSet, bool HasIndex)
      : base(LinkTypeMember.instance)
    {
      fName = Name;
      fIsSet = IsSet;
      fHasIndex = HasIndex;
    }

    private string fName;
    public override string Name
    {
      get { return fName; }
    }

    private bool fIsSet;
    public bool IsSet
    {
      get { return fIsSet; }
    }

    private bool fHasIndex;
    public bool HasIndex
    {
      get { return fHasIndex; }
    }

    #region Link

    public override int Size()
    {
      return 1 + Functions.SizeOf(fName);
    }

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteLinkByte((byte)LinkType.ID, true, fIsSet, fHasIndex);
      Writer.WriteString(fName);
    }

    #endregion

    protected internal override LinkData Invoke(LinkMessage Message, LinkStack SenderPath, Apartment Apartment, Servlet Servlet, LinkData DataIn)
    {
      //  Obtain the object
      object Object = Servlet.Object;
      //  Obtain the property
      PropertyInfo property = Object.GetType().GetProperty(Name);
      if (property == null)
        throw new EMorph("Property not found");
      //  Decode input
      object[] Index = null;
      object Value = null;
      if (DataIn != null)
        Parameters.Decode(Apartment.InstanceFactories, DataIn.ValueTypeStringData, DevicePathOf(SenderPath), DataIn.Reader, out Index, out Value);
      //  Invoke the property
      if (IsSet)
      {
        property.SetValue(Object, Value, Index);
        return null;
      }
      else
      {
        Value = property.GetValue(Object, Index);
        //  Encode output
        return new LinkData(Parameters.Encode(null, Value, Apartment.InstanceFactories));
      }
    }

    public override string ToString()
    {
      string result = "{Property ";
      if (IsSet)
        result += "Set ";
      else
        result += "Get ";
      result += "Name=\"" + Name + "\"";
      if (HasIndex)
        result += "[]";
      return result + "}"; ;
    }
  }
}