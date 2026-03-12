// Decompiled with JetBrains decompiler
// Type: DotNet35Random
// Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B32F9C0F-1A26-45C3-8038-96B8728A02F4
// Assembly location: H:\SteamLibrary\steamapps\common\Dyson Sphere Program\DSPGAME_Data\Managed\Assembly-CSharp.dll

using System;
using System.Runtime.InteropServices;

#nullable disable
[ComVisible(true)]
[Serializable]
public class DotNet35Random
{
  private const int MBIG = 2147483647 /*0x7FFFFFFF*/;
  private const int MSEED = 161803398;
  private const int MZ = 0;
  private int inext;
  private int inextp;
  private int[] SeedArray = new int[56];

  public DotNet35Random()
    : this(Environment.TickCount)
  {
  }

  public DotNet35Random(int Seed)
  {
    int num1 = 161803398 - Math.Abs(Seed);
    this.SeedArray[55] = num1;
    int num2 = 1;
    for (int index1 = 1; index1 < 55; ++index1)
    {
      int index2 = 21 * index1 % 55;
      this.SeedArray[index2] = num2;
      num2 = num1 - num2;
      if (num2 < 0)
        num2 += int.MaxValue;
      num1 = this.SeedArray[index2];
    }
    for (int index3 = 1; index3 < 5; ++index3)
    {
      for (int index4 = 1; index4 < 56; ++index4)
      {
        this.SeedArray[index4] -= this.SeedArray[1 + (index4 + 30) % 55];
        if (this.SeedArray[index4] < 0)
          this.SeedArray[index4] += int.MaxValue;
      }
    }
    this.inext = 0;
    this.inextp = 31 /*0x1F*/;
  }

  protected virtual double Sample()
  {
    if (++this.inext >= 56)
      this.inext = 1;
    if (++this.inextp >= 56)
      this.inextp = 1;
    int num = this.SeedArray[this.inext] - this.SeedArray[this.inextp];
    if (num < 0)
      num += int.MaxValue;
    this.SeedArray[this.inext] = num;
    return (double) num * 4.6566128752457969E-10;
  }

  public virtual int Next() => (int) (this.Sample() * (double) int.MaxValue);

  public virtual int Next(int maxValue)
  {
    if (maxValue < 0)
      throw new ArgumentOutOfRangeException(DotNet35Locale.GetText("Max value is less than min value."));
    return (int) (this.Sample() * (double) maxValue);
  }

  public virtual int Next(int minValue, int maxValue)
  {
    if (minValue > maxValue)
      throw new ArgumentOutOfRangeException(DotNet35Locale.GetText("Min value is greater than max value."));
    uint num = (uint) (maxValue - minValue);
    return num <= 1U ? minValue : (int) ((long) (uint) (this.Sample() * (double) num) + (long) minValue);
  }

  public virtual void NextBytes(byte[] buffer)
  {
    if (buffer == null)
      throw new ArgumentNullException(nameof (buffer));
    for (int index = 0; index < buffer.Length; ++index)
      buffer[index] = (byte) (this.Sample() * 256.0);
  }

  public virtual double NextDouble() => this.Sample();
}
