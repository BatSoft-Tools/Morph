using System;
using System.Collections;
using System.Collections.Generic;
using Morph.Params;

namespace Morph.Endpoint
{
  public class Device
  {
    internal Device(LinkStack Path)
    {
      fPath = Path;
    }

    internal Hashtable fApartmentProxiesByApartmentID = new Hashtable();

    internal LinkStack fPath;

    public TimeSpan DefaultTimeout = new TimeSpan(0, 1, 0);

    public ApartmentProxy Find(int ApartmentID)
    {
      lock (fApartmentProxiesByApartmentID)
        return (ApartmentProxy)fApartmentProxiesByApartmentID[ApartmentID];
    }

    public ApartmentProxy Obtain(int ApartmentID, InstanceFactories InstanceFactories)
    {
      lock (fApartmentProxiesByApartmentID)
      {
        ApartmentProxy result = Find(ApartmentID);
        if (result == null)
          result = new ApartmentProxy(this, ApartmentID, DefaultTimeout, InstanceFactories);
        return result;
      }
    }
  }

  public class Devices
  {
    static private List<Device> All = new List<Device>();

    static public Device Find(LinkStack Path)
    {
      for (int i = All.Count - 1; i >= 0; i--)
        if (Path.Equals(All[i].fPath))
          return All[i];
      return null;
    }

    static public Device Obtain(LinkStack Path)
    {
      lock (All)
      {
        Device result = Find(Path);
        if (result == null)
        {
          result = new Device(Path);
          All.Add(result);
        }
        return result;
      }
    }
  }
}