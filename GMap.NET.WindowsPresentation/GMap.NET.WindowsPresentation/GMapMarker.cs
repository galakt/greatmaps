﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System;
using System.Diagnostics;
using GMap.NET.WindowsPresentation.Interfaces;

namespace GMap.NET.WindowsPresentation
{
   /// <summary>
   /// GMap.NET marker
   /// </summary>
   public class GMapMarker : INotifyPropertyChanged, IDisposable, IClearable
   {
      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged(string name)
      {
         if(PropertyChanged != null)
         {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
         }
      }

      protected void OnPropertyChanged(PropertyChangedEventArgs name)
      {
         if(PropertyChanged != null)
         {
            PropertyChanged(this, name);
         }
      }

      private UIElement _shape;
      static readonly PropertyChangedEventArgs Shape_PropertyChangedEventArgs = new PropertyChangedEventArgs("Shape");

      /// <summary>
      /// marker visual
      /// </summary>
      public UIElement Shape
      {
         get
         {
            return _shape;
         }
         set
         {
            if(_shape != value)
            {
               _shape = value;
               OnPropertyChanged(Shape_PropertyChangedEventArgs);

               UpdateLocalPosition();
            }
         }
      }

      private PointLatLng _position;

      /// <summary>
      /// coordinate of marker
      /// </summary>
      public PointLatLng Position
      {
         get
         {
            return _position;
         }
         set
         {
            if(_position != value)
            {
               _position = value;
               UpdateLocalPosition();
            }
         }
      }

      private GMapControl _map;

      /// <summary>
      /// the map of this marker
      /// </summary>
      public GMapControl Map
      {
         get
         {
            if (Shape != null && _map == null)
            {
               DependencyObject visual = Shape;
               while (visual != null && !(visual is GMapControl))
               {
                  visual = VisualTreeHelper.GetParent(visual);
               }

               _map = visual as GMapControl;
            }

            return _map;
         }
         internal set { _map = value; }
      }

      /// <summary>
      /// custom object
      /// </summary>
      public object Tag;

      private System.Windows.Point _offset;
      /// <summary>
      /// offset of marker
      /// </summary>
      public System.Windows.Point Offset
      {
         get
         {
            return _offset;
         }
         set
         {
            if(_offset != value)
            {
               _offset = value;
               UpdateLocalPosition();
            }
         }
      }

      private int _localPositionX;
      static readonly PropertyChangedEventArgs LocalPositionX_PropertyChangedEventArgs = new PropertyChangedEventArgs("LocalPositionX");

      /// <summary>
      /// local X position of marker
      /// </summary>
      public int LocalPositionX
      {
         get
         {
            return _localPositionX;
         }
         internal set
         {
            if(_localPositionX != value)
            {
               _localPositionX = value;
               OnPropertyChanged(LocalPositionX_PropertyChangedEventArgs);
            }
         }
      }

      private int _localPositionY;
      static readonly PropertyChangedEventArgs LocalPositionY_PropertyChangedEventArgs = new PropertyChangedEventArgs("LocalPositionY");

      /// <summary>
      /// local Y position of marker
      /// </summary>
      public int LocalPositionY
      {
         get
         {
            return _localPositionY;
         }
         internal set
         {
            if(_localPositionY != value)
            {
               _localPositionY = value;
               OnPropertyChanged(LocalPositionY_PropertyChangedEventArgs);
            }
         }
      }

      private int _zIndex;
      static readonly PropertyChangedEventArgs ZIndex_PropertyChangedEventArgs = new PropertyChangedEventArgs("ZIndex");

      /// <summary>
      /// the index of Z, render order
      /// </summary>
      public int ZIndex
      {
         get
         {
            return _zIndex;
         }
         set
         {
            if(_zIndex != value)
            {
               _zIndex = value;
               OnPropertyChanged(ZIndex_PropertyChangedEventArgs);
            }
         }
      }

      public bool? AllowZoomLvlVisibilityValidation { get; set; }

      public GMapMarker(PointLatLng pos)
      {
         Position = pos;
      }

      internal GMapMarker()
      {
      }

      /// <summary>
      /// calls Dispose on _shape if it implements IDisposable, sets _shape to null and clears route
      /// </summary>
      public virtual void Clear()
      {
         var s = (Shape as IDisposable);
         if(s != null)
         {
            s.Dispose();
            s = null;
         }
         Shape = null;
      }

      /// <summary>
      /// updates marker position, internal access usualy
      /// </summary>
      void UpdateLocalPosition()
      {
         //Debug.WriteLine($"GMapMarker => UpdateLocalPosition. Map != null ? {Map != null}");
         if (Map != null)
         {
            GPoint p = Map.FromLatLngToLocal(Position);
            p.Offset(-(long)Map.MapTranslateTransform.X, -(long)Map.MapTranslateTransform.Y);

            LocalPositionX = (int)(p.X + (long)(Offset.X));
            LocalPositionY = (int)(p.Y + (long)(Offset.Y));
         }
      }

      /// <summary>
      /// forces to update local marker  position
      /// dot not call it if you don't really need to ;}
      /// </summary>
      /// <param name="m"></param>
      internal void ForceUpdateLocalPosition(GMapControl m)
      {
         //Debug.WriteLine($"GMapMarker => ForceUpdateLocalPosition. m != null ? {m != null}");
         if(m != null)
         {
            _map = m;
         }
         UpdateLocalPosition();
      }

      public void Dispose()
      {
         Clear();
      }
   }
}