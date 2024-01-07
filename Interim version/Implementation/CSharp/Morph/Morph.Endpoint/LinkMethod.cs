using System.Collections.Generic;
using System.Reflection;
using Morph.Lib;
using Morph.Params;

namespace Morph.Endpoint
{
  public class LinkMethod : LinkMember
  {
    public LinkMethod(string Name)
      : base(LinkTypeMember.instance)
    {
      fName = Name;
    }

    private string fName;
    public override string Name
    {
      get { return fName; }
    }

    #region Link

    public override int Size()
    {
      return 1 + Functions.SizeOf(Name);
    }

    public override void Write(StreamWriter Writer)
    {
      Writer.WriteInt8(Functions.ToLinkByte((byte)LinkType.ID, false, false, false));
      Writer.WriteString(Name);
    }

    #endregion

    protected internal override LinkData Invoke(LinkMessage Message, LinkStack SenderPath, Apartment Apartment, Servlet Servlet, LinkData DataIn)
    {
      //  Obtain the object
      object Object = Servlet.Object;
      //  Obtain the method
      MethodInfo method = Object.GetType().GetMethod(Name);
      if (method == null)
        throw new EMorph("Method not found");
      //  Decode input
      object[] ParamsIn = null;
      object Special = null;
      if (DataIn != null)
        Parameters.Decode(Apartment.InstanceFactories, DataIn.ValueTypeStringData, DevicePathOf(SenderPath), DataIn.Reader, out ParamsIn, out Special);
      //  Might insert Message as the first parameter
      if (Object is IMorphParameters)
      {
        List<object> Params = new List<object>((ParamsIn == null ? 0 : ParamsIn.Length) + 1);
        Params.Add(Message);
        if (ParamsIn != null)
          for (int i = 0; i < ParamsIn.Length; i++)
            Params.Add(ParamsIn[i]);
        ParamsIn = Params.ToArray();
      }
      //  Invoke the method
#if LOG_MESSAGES
      Log.Default.Add(LinkTypes.AppName);
      Log.Default.Add("Invoke: " + Name);
      Log.Default.Add(Log.nl);
#endif
      object result = method.Invoke(Object, ParamsIn);
      //  Encode output
      StreamWriter DataOutWriter;
      if (method.ReturnType == typeof(void))
        DataOutWriter = Parameters.Encode(null, Apartment.InstanceFactories);
      else
        DataOutWriter = Parameters.Encode(null, result, Apartment.InstanceFactories);
      //  Return output
      if (DataOutWriter == null)
        return null;
      else
        return new LinkData(DataOutWriter);
    }

    public override string ToString()
    {
      return "{Method Name=\"" + Name + "\"}";
    }
  }
}