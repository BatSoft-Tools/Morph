using System.Collections;
using System.Collections.Generic;

namespace Morph.Lib
{
  public interface RegisterItemID
  {
    int ID
    {
      get;
    }
  }

  public interface RegisterItemName
  {
    string Name
    {
      get;
    }
  }

  public class RegisterItems<T> : IEnumerable
  {
    #region Private

    private Hashtable fItems = new Hashtable();

    private void Add(object key, T item)
    {
      lock (fItems)
        if (!fItems.Contains(key))
          fItems.Add(key, item);
    }

    #endregion

    #region Public

    public int Count
    { get { return fItems.Count; } }

    public void Add(T item)
    {
      if (item is RegisterItemID)
        Add(((RegisterItemID)item).ID, item);
      if (item is RegisterItemName)
        Add(((RegisterItemName)item).Name, item);
    }

    public void Remove(T item)
    {
      lock (fItems)
      {
        if (item is RegisterItemID)
          fItems.Remove(((RegisterItemID)item).ID);
        if (item is RegisterItemName)
          fItems.Remove(((RegisterItemName)item).Name);
      }
    }

    public void Remove(object key)
    {
      T item = Find(key);
      if (item != null)
        Remove(item);
    }

    public virtual T Find(object key)
    {
      lock (fItems)
        return (T)fItems[key];
    }

    public List<T> List()
    {
      List<T> Result = new List<T>();
      IEnumerator enums = fItems.GetEnumerator();
      while (enums.MoveNext())
        Result.Add((T)((DictionaryEntry)enums.Current).Value);
      return Result;
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
      return fItems.GetEnumerator();
    }

    #endregion
  }
}