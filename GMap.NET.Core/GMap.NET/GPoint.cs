
namespace GMap.NET
{
   using System.Globalization;
   using System;
    using System.Collections.Generic;

   /// <summary>
   /// the point ;}
   /// </summary>
   [Serializable]
   public struct GPoint
   {
      public static readonly GPoint Empty = new GPoint();

      private long x;
      private long y;
      private object _data;
      private decimal? _weight;

      public GPoint(long x, long y)
      {
         this.x = x;
         this.y = y;
         _data = null;
         _weight = null;
      }

      public GPoint(long x, long y, object data, decimal? weight)
      {
         this.x = x;
         this.y = y;
         _data = data;
         _weight = weight;
      }
      
      public GPoint(GSize sz)
      {
         this.x = sz.Width;
         this.y = sz.Height;
         _data = null;
         _weight = null;
      }

      //public GPoint(int dw)
      //{
      //   this.x = (short) LOWORD(dw);
      //   this.y = (short) HIWORD(dw);
      //}
      public object Data { get { return _data; } set { _data = value; } }
      public decimal? Weight { get { return _weight; } set { _weight = value; } }

      public bool IsEmpty
      {
         get
         {
            return x == 0 && y == 0;
         }
      }

      public long X
      {
         get
         {
            return x;
         }
         set
         {
            x = value;
         }
      }

      public long Y
      {
         get
         {
            return y;
         }
         set
         {
            y = value;
         }
      }

      public static explicit operator GSize(GPoint p)
      {
         return new GSize(p.X, p.Y);
      }

      public static GPoint operator+(GPoint pt, GSize sz)
      {
         return Add(pt, sz);
      }

      public static GPoint operator-(GPoint pt, GSize sz)
      {
         return Subtract(pt, sz);
      }

      public static bool operator==(GPoint left, GPoint right)
      {
         return left.X == right.X && left.Y == right.Y;
      }

      public static bool operator!=(GPoint left, GPoint right)
      {
         return !(left == right);
      }

      public static GPoint Add(GPoint pt, GSize sz)
      {
         return new GPoint(pt.X + sz.Width, pt.Y + sz.Height);
      }

      public static GPoint Subtract(GPoint pt, GSize sz)
      {
         return new GPoint(pt.X - sz.Width, pt.Y - sz.Height);
      }

      public override bool Equals(object obj)
      {
         if(!(obj is GPoint))
            return false;
         GPoint comp = (GPoint) obj;
         return comp.X == this.X && comp.Y == this.Y;
      }

      public override int GetHashCode()
      {
         return (int)(x ^ y);
      }

      public void Offset(long dx, long dy)
      {
         X += dx;
         Y += dy;
      }

      public void Offset(GPoint p)
      {
         Offset(p.X, p.Y);
      }
      public void OffsetNegative(GPoint p)
      {
         Offset(-p.X, -p.Y);
      }

      public override string ToString()
      {
         return "{X=" + X.ToString(CultureInfo.CurrentCulture) + ",Y=" + Y.ToString(CultureInfo.CurrentCulture) + "}";
      }

      //private static int HIWORD(int n)
      //{
      //   return (n >> 16) & 0xffff;
      //}

      //private static int LOWORD(int n)
      //{
      //   return n & 0xffff;
      //}
   }

   internal class GPointComparer : IEqualityComparer<GPoint>
   {
       public bool Equals(GPoint x, GPoint y)
       {
           return x.X == y.X && x.Y == y.Y;
       }

       public int GetHashCode(GPoint obj)
       {
           return obj.GetHashCode();
       }
   }
}
