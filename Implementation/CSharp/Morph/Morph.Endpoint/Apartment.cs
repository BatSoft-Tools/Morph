#region Description
/* Essentially, an apartment represents a logical memory space on a device.
 * Servlets represent objects within that memory space.
 * 
 * For any remote procedure call, an apartment represents the called endpoint.
 * So, a method call will be sent from an apartment proxy to an apartment, and
 * the method reply will be sent from the apartment to the apartment proxy.
 */
#endregion

#region Client side usage
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using Morph.Lib;
using Morph.Lib.LinkedList;
using Morph.Params;

namespace Morph.Endpoint
{
  public class Apartment : RegisterItemID, IDisposable
  {
    public Apartment(InstanceFactories InstanceFactories)
      : this(InstanceFactories, null)
    {
    }

    public Apartment(InstanceFactories InstanceFactories, object DefaultObject)
    {
      _InstanceFactories = InstanceFactories;
      if (InstanceFactories == null)
        throw new EMorphUsage("Apartment must have InstanceFactories object");
      fOwner = null;
      fID = IDFactory.Generate();
      fServlets = new Servlets(this, DefaultObject);
      ApartmentFactory.RegisterApartment(this);
    }

    internal Apartment(ApartmentFactory Owner, object DefaultObject)
    {
      if (DefaultObject == null)
        throw new EMorphUsage("Cannot create an apartment without a default service object");
      _InstanceFactories = Owner.InstanceFactories;
      if (_InstanceFactories == null)
        throw new EMorphUsage("Apartment must have an InstanceFactories object");
      fOwner = Owner;
      fID = IDFactory.Generate();
      fServlets = new Servlets(this, DefaultObject);
      ApartmentFactory.RegisterApartment(this);
    }

    #region IDisposable Members

    public virtual void Dispose()
    {
      try
      {
        IDFactory.Release(ID);
      }
      finally
      {
        ApartmentFactory.UnregisterApartment(this);
      }
    }

    #endregion

    #region RegisterItemID Members

    private int fID;
    public int ID
    {
      get { return fID; }
    }

    #endregion

    public const int DefaultID = 0;

    static public IIDFactory IDFactory = new IDSeed(DefaultID + 1);

    private ApartmentFactory fOwner;
    public ApartmentFactory Owner
    {
      get { return fOwner; }
    }

    public virtual void ResetTimeout()
    {
      //  May be used by subclass
    }

    public virtual Servlet DefaultServlet
    {
      get { return Servlets.Find(Servlet.DefaultID); }
    }

    private Servlets fServlets;
    public Servlets Servlets
    {
      get { return fServlets; }
    }

    private InstanceFactories _InstanceFactories;
    public InstanceFactories InstanceFactories
    {
      get { return _InstanceFactories; }
    }

    internal virtual void AppendSequenceLinks(LinkStack linkStack)
    {
      //  Not implemented here
    }
  }

  public interface DefaultServletObjectFactory
  {
    object ObtainServlet();
  }

  public abstract class ApartmentFactory
  {
    protected ApartmentFactory(InstanceFactories InstanceFactories)
    {
      fInstanceFactories = InstanceFactories;
    }

    internal Service fService;

    private InstanceFactories fInstanceFactories;
    public InstanceFactories InstanceFactories
    {
      get { return fInstanceFactories; }
    }

    static private RegisterItems<Apartment> All = new RegisterItems<Apartment>();

    static internal void RegisterApartment(Apartment Apartment)
    {
      lock (All)
        ApartmentFactory.All.Add(Apartment);
    }

    static internal void UnregisterApartment(Apartment Apartment)
    {
      lock (All)
        ApartmentFactory.All.Remove(Apartment);
    }

    static public Apartment Find(int ApartmentID)
    {
      lock (All)
        return All.Find(ApartmentID);
    }

    static public Apartment Obtain(int ApartmentID)
    {
      if (ApartmentID == Apartment.DefaultID)
        throw new EMorph("Obtain a default apartment by specifing a service.");
      //  Lookup existing apartment
      Apartment apartment = Find(ApartmentID);
      if (apartment == null)
        throw new EMorph("Apartment does not exist");
      //  Keep session apartments alive
      apartment.ResetTimeout();
      return apartment;
    }

    public abstract Apartment ObtainDefault();

    protected internal virtual void ShutDown()
    {
      lock (All)
      {
        List<Apartment> AllApartments = All.List();
        foreach (Apartment apartment in AllApartments)
          apartment.Dispose();
      }
    }
  }
}