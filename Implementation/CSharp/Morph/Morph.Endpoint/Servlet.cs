using System.Collections;
using Morph.Lib;

namespace Morph.Endpoint
{
  public class Servlet
  {
    internal Servlet(Apartment Apartment, int ID, object Object, string TypeName)
    {
      fApartment = Apartment;
      fID = ID;
      fObject = Object;
      fTypeName = TypeName;
    }

    private Apartment fApartment;
    public Apartment Apartment
    {
      get { return fApartment; }
    }

    public const int DefaultID = 0;

    private int fID;
    public int ID
    {
      get { return fID; }
    }

    private object fObject;
    public object Object
    {
      get { return fObject; }
    }

    private string fTypeName;
    public string TypeName
    {
      get { return fTypeName; }
    }
  }

  public class Servlets
  {
    internal Servlets(Apartment Apartment, object DefaultServletObject)
    {
      fApartment = Apartment;
      fDefault = Obtain(DefaultServletObject);
    }

    private Apartment fApartment;

    private IDSeed fServletIDSeed = new IDSeed(Servlet.DefaultID);
    private Hashtable fServlets = new Hashtable();

    private Servlet fDefault;
    public Servlet Default
    {
      get { return fDefault; }
    }
    
    public Servlet Obtain(object ServletObject)
    {
      return Obtain(ServletObject, null);
    }
    
    public Servlet Obtain(object ServletObject, string TypeName)
    {
      Servlet servlet = new Servlet(fApartment, fServletIDSeed.Generate(), ServletObject, TypeName);
      fServlets.Add(servlet.ID, servlet);
      return servlet;
    }

    public void Remove(int ServletID)
    {
      if (ServletID == fDefault.ID)
        throw new EMorphUsage("Cannot deregister default servlet");
      fServlets.Remove(ServletID);
    }

    public Servlet Find(int ServletID)
    {
      if (ServletID == fDefault.ID)
        return Default;
      return (Servlet)(fServlets[ServletID]);
    }
  }
}