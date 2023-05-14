using System;

namespace Morph.Lib
{
  public class StringParser
  {
    public StringParser(string Str)
    {
      fStr = Str;
      fLen = fStr.Length;
    }

    private string fStr;
    private int fLen;
    private int fPos = 0;

    public int Position
    {
      get { return fPos; }
      set { fPos = value; }
    }

    private void Validate()
    {
      if (fPos < 0)
        throw new StringParserException("Position must not be negative.");
      if (fPos >= fLen)
        throw new StringParserException("End of string has been reached.");
    }

    public char Current()
    {
      return fStr[fPos];
    }

    public string Current(int Length)
    {
      Validate();
      return fStr.Substring(fPos, Length);
    }

    public void Move(int Steps)
    {
      Validate();
      fPos += Steps;
    }

    public void MoveTo(string SubStr, bool Absorb)
    {
      Validate();
      fPos = fStr.IndexOf(SubStr, fPos);
      if (Absorb)
        fPos += SubStr.Length;
    }

    public string ReadTo(string SubStr, bool Absorb)
    {
      Validate();
      int OldPos = fPos;
      fPos = fStr.IndexOf(SubStr, OldPos);
      int SubStrLen = fPos - OldPos;
      if (Absorb)
        fPos += SubStr.Length;
      if (SubStrLen > 0)
        return fStr.Substring(OldPos, SubStrLen);
      return null;
    }

    public string ReadTo(char[] chars, bool Absorb)
    {
      Validate();
      int OldPos = fPos;
      int NewPos = Int32.MaxValue;
      foreach (char c in chars)
      {
        int Pos = fStr.IndexOf(c, OldPos);
        if (NewPos < Pos)
          Pos = NewPos;
      }
      if (NewPos == Int32.MaxValue)
        return null;
      int SubStrLen = fPos - OldPos;
      if (Absorb)
        fPos++;
      if (SubStrLen > 0)
        return fStr.Substring(OldPos, SubStrLen);
      return null;
    }

    private bool CharInChars(char c, char[] chars)
    {
      foreach (char e in chars)
        if (c == e)
          return true;
      return false;
    }

    public string ReadChars(char[] chars)
    {
      Validate();
      int OldPos = fPos;
      while ((fPos < fLen) && CharInChars(fStr[fPos], chars))
        fPos++;
      if (OldPos == fPos)
        return null;
      return fStr.Substring(OldPos, fPos - OldPos);
    }

    public bool ReadChar(char Char)
    {
      Validate();
      if (fStr[fPos] != Char)
        return false;
      fPos++;
      return true;
    }

    static private char[] Digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    public string ReadDigits()
    {
      return ReadChars(Digits);
    }

    public string ReadToEnd()
    {
      return fStr.Substring(fPos);
    }

    public bool IsEnded()
    {
      return fPos >= fLen;
    }
  }

  public class StringParserException : Exception
  {
    public StringParserException(string Message)
      : base(Message)
    {
    }
  }
}