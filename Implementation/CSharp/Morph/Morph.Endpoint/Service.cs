#region Server side usage
/* There are two ways for a service to provide an apartment.  A service may provide:
 * - a single *shared apartment* that replies to all calls
 * - a dedicated *session apartment* that communicates with only one "client"
 * 
 * Lifetime:
 * ---------
 * Session apartments are given a lifetime.  If no communication occurs during the timeout
 * then the apartment (ie. session) is lost.  Any communication to that apartment will 
 * reset the timer.
 * 
 * Default Servlet Object:
 * -----------------------
 * An service must have a default object.
 * The SOAP equivalent is its published interface.
 * 
 * For a service that provides a shared apartment, a single object will suffice.
 * For a service that provides session apartments, one might want to provide a unique
 * object for each session.  For this, implement the DefaultServletObjectFactory interface.
 * 
 * Apartment factories:
 * --------------------
 * A service will get an apartment from an factory.  Supply an ApartmentFactory factory to the
 * constructor of a service.
 */
#endregion

#region Examples
/* In these examples we publish a service named "Test".
 * The object to be published is of type TestClass.
 * The DefaultServletObjectFactory implementation that creates TestClass objects is TestFactory.
 * 
 * 1. Creating a shared service.  This is overkill, unless you want to do something fancy.
 * Services.Register("Test", new ApartmentFactoryShared(new TestFactory()));
 * 
 * 2. We could use the factory that is already provided, which simply returns the given TestClass object.
 * Services.Register("Test", new ApartmentFactoryShared(new DefaultServletObjectFactoryShared(new TestClass())));
 * 
 * 3. The easiest is to use the shortform:
 * Services.Register("Test", new ApartmentFactoryShared(new TestClass()));
 * 
 * 4. Starting a sessioned sevice with a 5 minute idleness timeout:
 * Services.Register("Test", new ApartmentFactorySession(new TestFactory(), new TimeSpan(0, 5, 0)));
 */
#endregion

using Morph.Lib;

namespace Morph.Endpoint
{
  public class Service : RegisterItemID, RegisterItemName
  {
    internal Service(string Name, ApartmentFactory ApartmentFactory)
    {
      if ((Name == null) || (Name.Length == 0))
        throw new EMorphUsage("A service needs a proper name.");
      if (ApartmentFactory == null)
        throw new EMorphUsage("A service needs an Apartments strategy.");
      if (ApartmentFactory.fService != null)
        throw new EMorphUsage("Services cannot share the same ApartmentFactory object.");
      fName = Name.ToLower();
      fApartmentFactory = ApartmentFactory;
      fApartmentFactory.fService = this;
    }

    #region MorphItemID Members

    public int ID
    {
      get { return fName.GetHashCode(); }
    }

    #endregion

    #region MorphItemName Members

    private string fName;
    public string Name
    {
      get { return fName; }
    }

    #endregion

    private ApartmentFactory fApartmentFactory;
    public ApartmentFactory ApartmentFactory
    {
      get { return fApartmentFactory; }
    }

    public void Deregister()
    {
      Services.Deregister(this);
    }
  }

  public class Services : RegisterItems<Service>
  {
    static private RegisterItems<Service> All = new RegisterItems<Service>();

    static public Service Register(string Name, ApartmentFactory ApartmentFactory)
    {
      Service service = new Service(Name.ToLower(), ApartmentFactory);
      lock (All)
        All.Add(service);
      return service;
    }

    static public void Deregister(string Name)
    {
      lock (All)
        Deregister(All.Find(Name.ToLower()));
    }

    static public void Deregister(Service service)
    {
      if (service == null)
        throw new EMorphUsage("Cannot stop a non-registered service");
      lock (All)
        All.Remove(service);
      service.ApartmentFactory.ShutDown();
    }

    static public Service Obtain(string Name)
    {
      Service service;
      lock (All)
        service = All.Find(Name.ToLower());
      if (service == null)
        throw new EMorph("Service not available: \"" + Name + '\"');
      return service;
    }
  }
}