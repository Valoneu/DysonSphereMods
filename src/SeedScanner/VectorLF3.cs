
using System;
using UnityEngine;
#nullable disable
[Serializable]
public struct VectorLF3
{
  public double x;
  public double y;
  public double z;
  public static VectorLF3 zero => new VectorLF3(0.0f, 0.0f, 0.0f);
  public static VectorLF3 one => new VectorLF3(1f, 1f, 1f);
  public static VectorLF3 minusone => new VectorLF3(-1f, -1f, -1f);
  public static VectorLF3 unit_x => new VectorLF3(1f, 0.0f, 0.0f);
  public static VectorLF3 unit_y => new VectorLF3(0.0f, 1f, 0.0f);
  public static VectorLF3 unit_z => new VectorLF3(0.0f, 0.0f, 1f);
  public VectorLF2 xy => new VectorLF2(this.x, this.y);
  public VectorLF2 yx => new VectorLF2(this.y, this.x);
  public VectorLF2 zx => new VectorLF2(this.z, this.x);
  public VectorLF2 xz => new VectorLF2(this.x, this.z);
  public VectorLF2 yz => new VectorLF2(this.y, this.z);
  public VectorLF2 zy => new VectorLF2(this.z, this.y);
  public VectorLF3(VectorLF3 vec)
  {
    this.x = vec.x;
    this.y = vec.y;
    this.z = vec.z;
  }
  public VectorLF3(double x_, double y_, double z_)
  {
    this.x = x_;
    this.y = y_;
    this.z = z_;
  }
  public VectorLF3(float x_, float y_, float z_)
  {
    this.x = (double) x_;
    this.y = (double) y_;
    this.z = (double) z_;
  }
  public static bool operator ==(VectorLF3 lhs, VectorLF3 rhs)
  {
    return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
  }
  public static bool operator !=(VectorLF3 lhs, VectorLF3 rhs)
  {
    return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z;
  }
  public static VectorLF3 operator *(VectorLF3 lhs, VectorLF3 rhs)
  {
    return new VectorLF3(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z);
  }
  public static VectorLF3 operator *(VectorLF3 lhs, double rhs)
  {
    return new VectorLF3(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
  }
  public static VectorLF3 operator /(VectorLF3 lhs, double rhs)
  {
    return new VectorLF3(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
  }
  public static VectorLF3 operator -(VectorLF3 vec) => new VectorLF3(-vec.x, -vec.y, -vec.z);
  public static VectorLF3 operator -(VectorLF3 lhs, VectorLF3 rhs)
  {
    return new VectorLF3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
  }
  public static VectorLF3 operator +(VectorLF3 lhs, VectorLF3 rhs)
  {
    return new VectorLF3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
  }
  public static implicit operator VectorLF3(Vector3 vec3) => new VectorLF3(vec3.x, vec3.y, vec3.z);
  public static implicit operator Vector3(VectorLF3 vec3)
  {
    return new Vector3((float) vec3.x, (float) vec3.y, (float) vec3.z);
  }
  public double sqrMagnitude => this.x * this.x + this.y * this.y + this.z * this.z;
  public double magnitude => Math.Sqrt(this.x * this.x + this.y * this.y + this.z * this.z);
  public double Distance(VectorLF3 vec)
  {
    return Math.Sqrt((vec.x - this.x) * (vec.x - this.x) + (vec.y - this.y) * (vec.y - this.y) + (vec.z - this.z) * (vec.z - this.z));
  }
  public override bool Equals(object obj)
  {
    return obj != null && obj is VectorLF3 vectorLf3 && this.x == vectorLf3.x && this.y == vectorLf3.y && this.z == vectorLf3.z;
  }
  public override int GetHashCode() => base.GetHashCode();
  public override string ToString() => $"[{this.x},{this.y},{this.z}]";
  public static double Dot(VectorLF3 a, VectorLF3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
  public static VectorLF3 Cross(VectorLF3 a, VectorLF3 b)
  {
    return new VectorLF3(a.y * b.z - b.y * a.z, a.z * b.x - b.z * a.x, a.x * b.y - b.x * a.y);
  }
  public static double AngleRAD(VectorLF3 a, VectorLF3 b)
  {
    VectorLF3 normalized1 = a.normalized;
    VectorLF3 normalized2 = b.normalized;
    double d = normalized1.x * normalized2.x + normalized1.y * normalized2.y + normalized1.z * normalized2.z;
    if (d > 1.0)
      d = 1.0;
    else if (d < -1.0)
      d = -1.0;
    return Math.Acos(d);
  }
  public static double AngleDEG(VectorLF3 a, VectorLF3 b)
  {
    VectorLF3 normalized1 = a.normalized;
    VectorLF3 normalized2 = b.normalized;
    double d = normalized1.x * normalized2.x + normalized1.y * normalized2.y + normalized1.z * normalized2.z;
    if (d > 1.0)
      d = 1.0;
    else if (d < -1.0)
      d = -1.0;
    return Math.Acos(d) / Math.PI * 180.0;
  }
  public static VectorLF3 MoveTowards(VectorLF3 current, VectorLF3 target, double maxDistanceDelta)
  {
    VectorLF3 vectorLf3 = target - current;
    double magnitude = vectorLf3.magnitude;
    return magnitude <= maxDistanceDelta || magnitude == 0.0 ? target : current + vectorLf3 / magnitude * maxDistanceDelta;
  }
  public VectorLF3 normalized
  {
    get
    {
      double d = this.x * this.x + this.y * this.y + this.z * this.z;
      if (d < 1E-34)
        return new VectorLF3(0.0f, 0.0f, 0.0f);
      double num = Math.Sqrt(d);
      return new VectorLF3(this.x / num, this.y / num, this.z / num);
    }
  }
}
