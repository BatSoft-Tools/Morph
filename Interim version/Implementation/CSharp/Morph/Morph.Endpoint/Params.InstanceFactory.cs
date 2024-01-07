/**
 * When messages arrive at an endpoint, the endpoint will often need to translate
 * parameters into actual instances.  That is the role of instance factories.
 * 
 * Note:  The class InstanceFactories is not thread safe, so add all 
 * factories to it before supplying it to a service or apartment proxy.
 * Instance factories need to be thread safe, though this will rarely be an issue.
 * 
 * Note:  Because structs are so limited in functionality, a general solution to 
 * handle all structs is possible (see class InstanceFactoryStruct).
 * On the other hand, class instances may be too compex due to differences
 * in constructors, internal behaviour with getters and setters and so on.
 * In these cases developers will have to make their own IInstanceFactory's that 
 * can take into account the complexity of each class.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Morph.Endpoint;

namespace Morph.Params
{
  /** IReferenceFactory
   * 
   * An IReferenceFactory creates a business object proxy to encapsulate a servlet.
   * 
   * Example:
   *   public class MyInterfaceProxy : MyInterface
   *   {
   *     internal MyInterfaceProxy(ServletProxy ServletProxy)
   *     {
   *       fServletProxy = ServletProxy;
   *     }
   * 
   *     private ServletProxy fServletProxy;
   * 
   *     #region MyInterface Members
   * 
   *     public AnyType1 myMethod1(AnyType2 anyParam2, AnyType3 anyParam3)
   *     {
   *       return (AnyType1)fServletProxy.CallMethod("myMethod1", new object[1] { anyParam2, anyParam3 });
   *     }
   * 
   *     public AnyType3 myProperty1
   *     {
   *       get { return (AnyType3)fServletProxy.CallGetProperty("myProperty1", null); }
   *     }
   * 
   *     #endregion
   *   }
   */
  public interface IReferenceFactory
  {
    bool CreateReference(ServletProxy Value, out object Reference);
  }

  /** IInstanceFactory
   * 
   * An IInstanceFactory creates an instance based on the supplied Value.
   * 
   * This is really the inverse of IMorphInstance.  A complex object can
   * encode itself using IMorphInstance.MorphEncode(), then on the
   * receiving side, an IInstanceFactory can convert the ValueInstance
   * back to the object as desired.
   * 
   */
  public interface IInstanceFactory
  {
    bool CreateInstance(ValueInstance Value, out object Instance);
  }

  /** ISimpleFactory
   * 
   * A simple factory has the same purpose as IInstanceFactory, except that
   * the Value parameter is limited to the following simple types:
   * - Int8 (Byte)
   * - Int16
   * - Int32
   * - Int64
   * - Char
   * - String
   * - array of any of the above types.
   * 
   * For examples, see the implementations of predefined types.
   */
  public interface ISimpleFactory
  {
    bool EncodeSimple(out object Value, out string TypeName, object Instance);
    bool DecodeSimple(object Value, string TypeName, out object Instance);
  }

  #region Useful general implementations

  /**
   * This is meant to be commonly used for handling struct types automatically.
   * Add the struct types using AddStructType() and add to an InstanceFactories.
   * 
   * Note:  This is intended to work only for struct types.  However, it also works
   * for class types that look like structs.  This means having:
   * - a contructor without parameters
   * - fields (InstanceFactoryStruct ignores properties)
   * 
   * Examples:
   *  AddStructType(typeof(MyStruct));
   *  AddStructType(typeof(MyClass));
   */
  public class InstanceFactoryStruct : IInstanceFactory
  {
    #region Private

    private Hashtable fTypes = new Hashtable();

    private Type FindType(string ValueTypeName)
    {
      if (ValueTypeName == null)
        return null;
      else
        return (Type)fTypes[ValueTypeName];
    }

    #endregion

    public void AddStructType(Type type)
    {
      fTypes.Add(type.Name, type);
    }

    #region IInstanceFactory Members

    static private Type[] NoParams = new Type[0];

    public bool CreateInstance(ValueInstance Value, out object Instance)
    {
      Instance = null;
      //  Find a type we are able to convert to
      Type type = FindType(Value.TypeName);
      if (type == null)
        return false;
      //  Create the result instance
      ConstructorInfo constructor = type.GetConstructor(NoParams);
      if (constructor != null)
        Instance = constructor.Invoke(null);
      else
        Instance = Activator.CreateInstance(type);
      //  Read the parameters into the instance
      for (int i = Value.Struct.Count - 1; i >= 0; i--)
        type.GetField(Value.Struct.Names[i]).SetValue(Instance, Value.Struct.Values[i]);
      //  Return success
      return true;
    }

    #endregion
  }

  /**
   * 
   * This is meant to be commonly used for handling array types automatically.
   * Add the array element type using AddArrayElemType() and add to an InstanceFactories.
   * 
   * Examples:
   *  AddArrayElemType(typeof(int));  //  Converts to int[]
   *  AddArrayElemType(typeof(MyClass));  //  Converts to MyClass[]
   */
  public class InstanceFactoryArray : IInstanceFactory
  {
    #region Private

    private Hashtable fTypes = new Hashtable();

    private Type FindType(string ValueTypeName)
    {
      if (ValueTypeName == null)
        return null;
      else
        return (Type)fTypes[ValueTypeName];
    }

    #endregion

    public void AddArrayElemType(Type type)
    {
      fTypes.Add(type.Name + "[]", type);
    }

    #region IInstanceFactory Members

    public bool CreateInstance(ValueInstance Value, out object Instance)
    {
      Instance = null;
      //  This factory only deals with "pure" arrays
      if ((Value.Array == null) || (Value.Struct != null))
        return false;
      //  Find a type we are able to convert to
      Type type = FindType(Value.TypeName);
      if (type == null)
        return false;
      //  Get as object[]
      object[] values = Value.Array.Values.ToArray();
      //  Convert to <type>[]
      Array array = Array.CreateInstance(type, values.Length);
      values.CopyTo(array, 0);
      //  Complete
      Instance = array;
      return true;
    }

    #endregion
  }

  #endregion

  #region Predefined types

  internal class SimpleFactoryBool : ISimpleFactory
  {
    #region ISimpleFactory Members

    private const string TypeNameBool = "Bool";
    private const Byte True = 0xFF;
    private const Byte False = 0x00;

    public bool EncodeSimple(out object Value, out string TypeName, object Instance)
    {
      TypeName = TypeNameBool;
      if (Instance is Boolean)
      {
        if ((Boolean)Instance)
          Value = True; //  True as Byte 
        else
          Value = False; //  False as Byte 
        return true;
      }
      else
      {
        Value = null;
        return false;
      }
    }

    public bool DecodeSimple(object Value, string TypeName, out object Instance)
    {
      if (TypeNameBool.Equals(TypeName))
      {
        Instance = (Byte)Value != 0;
        return true;
      }
      else
      {
        Instance = null;
        return false;
      }
    }

    #endregion
  }

  internal class SimpleFactoryDateTime : ISimpleFactory
  {
    #region ISimpleFactory Members

    private const string TypeNameDateTime = "DateTime";

    public bool EncodeSimple(out object Value, out string TypeName, object Instance)
    {
      TypeName = TypeNameDateTime;
      if (Instance is DateTime)
      {
        Value = Morph.Lib.Conversion.DateTimeToStr((DateTime)Instance);
        return true;
      }
      else
      {
        Value = null;
        return false;
      }
    }

    public bool DecodeSimple(object Value, string TypeName, out object Instance)
    {
      if ("DateTime".Equals(TypeName))
      {
        Instance = Morph.Lib.Conversion.StrToDateTime((String)Value);
        return true;
      }
      else
      {
        Instance = null;
        return false;
      }
    }

    #endregion
  }

  internal class SimpleFactoryNotSupported : ISimpleFactory
  {
    #region ISimpleFactory Members

    public bool EncodeSimple(out object Value, out string TypeName, object Instance)
    {
      Value = null;
      TypeName = null;
      return false;
    }

    public bool DecodeSimple(object Value, string TypeName, out object Instance)
    {
      if ("Date".Equals(TypeName) || "Time".Equals(TypeName) || "Currency".Equals(TypeName))
        throw new EMorph("The type " + TypeName + " is not supported on this implementation.");
      Instance = null;
      return false;
    }

    #endregion
  }

  #endregion

  /** InstanceFactories
   * 
   * InstanceFactories loops through all factories of a certain type until either:
   * - there are no factories left to try
   * - a factory returns true, meaning that a conversion was successfully dealt with.
   */
  public class InstanceFactories
  {
    public InstanceFactories()
    {
      Add(FactoryBool);
      Add(FactoryDateTime);
      Add(FactoryNotSupported);
    }

    #region Internal

    #region Predefined types

    static private ISimpleFactory FactoryBool = new SimpleFactoryBool();
    static private ISimpleFactory FactoryDateTime = new SimpleFactoryDateTime();
    static private ISimpleFactory FactoryNotSupported = new SimpleFactoryNotSupported();

    #endregion

    private List<IReferenceFactory> fReferenceFactories = new List<IReferenceFactory>();
    private List<IInstanceFactory> fInstanceFactories = new List<IInstanceFactory>();
    private List<ISimpleFactory> fSimpleFactories = new List<ISimpleFactory>();

    internal bool CreateReference(ServletProxy Value, out object Reference)
    {
      //  If the ServletProxy has a facade, then use that
      Reference = Value.Facade;
      if (Reference != null)
        return true;
      //  If none found, then create a new one
      for (int i = 0; i < fReferenceFactories.Count; i++)
        if (fReferenceFactories[i].CreateReference(Value, out Reference))
        {
          Value.Facade = Reference;
          return true;
        }
      //  Oh well, just return the ServletProxy itself then
      Reference = Value;
      return false;
    }

    internal bool CreateInstance(ValueInstance Value, out object Instance)
    {
      for (int i = 0; i < fInstanceFactories.Count; i++)
        if (fInstanceFactories[i].CreateInstance(Value, out Instance))
          return true;
      Instance = Value;
      return false;
    }

    internal bool EncodeSimple(out object Value, out string TypeName, object Instance)
    {
      for (int i = 0; i < fSimpleFactories.Count; i++)
        if (fSimpleFactories[i].EncodeSimple(out Value, out TypeName, Instance))
          return true;
      TypeName = null;
      Value = Instance;
      return false;
    }

    internal bool DecodeSimple(object Value, string TypeName, out object Reference)
    {
      if (TypeName != null)
        for (int i = 0; i < fSimpleFactories.Count; i++)
          if (fSimpleFactories[i].DecodeSimple(Value, TypeName, out Reference))
            return true;
      Reference = Value;
      return false;
    }

    #endregion

    public void Add(IReferenceFactory Factory)
    {
      fReferenceFactories.Add(Factory);
    }

    public void Add(IInstanceFactory Factory)
    {
      fInstanceFactories.Add(Factory);
    }

    public void Add(ISimpleFactory Factory)
    {
      fSimpleFactories.Add(Factory);
    }
  }

  /** IMorphParameters
   * 
   * Add this to any server side business object to insert the LinkMessage as the 1st parameter.
   * Other than that, the methods are called as usual.
   * 
   * Example:
   *  The recieving business object implements IMorphParameters, so...
   *  the caller calls:       void MyMethod(string Str, int Num)
   *  the invoked method is:  void MyMethod(LinkMessage Message, string Str, int Num)
   * 
   * Note: This only applies to methods, not properties.
   */
  public interface IMorphParameters
  {
  }
}