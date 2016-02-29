using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Windows;

namespace GMap.NET.WindowsPresentation
{
   [Serializable]
   public class GMapOverlay : ISerializable, IDeserializationCallback, IDisposable
   {
      private bool _isActive;
      private GMapControl _mapControl;

      public string OverlayId { get; set; }
      public string OverlayName { get; set; }
      
      //todo: Must make thread safe!
      /// <summary>
      /// List of markers, should be thread safe
      /// </summary>
      public readonly ObservableCollection<GMapMarker> Markers = new ObservableCollection<GMapMarker>();
      /// <summary>
      /// List of routes, should be thread safe
      /// </summary>
      public readonly ObservableCollection<GMapRoute> Routes = new ObservableCollection<GMapRoute>();
      /// <summary>
      /// List of polygons, should be thread safe
      /// </summary>
      public readonly ObservableCollection<GMapPolygon> Polygons = new ObservableCollection<GMapPolygon>();
      /// <summary>
      /// Enable layer to show on map
      /// </summary>
      public bool IsActive
      {
         get { return _isActive; }
         set
         {
            _isActive = value;
            OnIsActiveChanged();
         }
      }
      /// <summary>
      /// Allow hide layer`s elements if current map zoom more MaxLayerZoomLvl or map zoom less MinLayerZoomLvl
      /// </summary>
      public bool AllowZoomLvlVisibilityValidation { get; set; }
      public int MaxLayerZoomLvl { get; set; }
      public int MinLayerZoomLvl { get; set; }
      /// <summary>
      /// ZIndex of layer and markers on layer
      /// </summary>
      public int ZIndex { get; set; }
      internal GMapControl MapControl
      {
         get { return _mapControl; }
         set
         {
            _mapControl = value;
            OnMapControlChanged();
         }
      }

      public GMapOverlay()
      {
         CreateEvents();
      }

      public GMapOverlay(string overlayId)
      {
         OverlayId = overlayId;
         CreateEvents();
      }

      protected virtual void OnMapControlChanged()
      {
         if (MapControl == null)
         {
            return;
         }

         foreach (var layersMarker in Markers)
         {
            if (!MapControl.Markers.Contains(layersMarker))
            {
               MapControl.Markers.Add(layersMarker);
            }
         }

         foreach (var layerRoute in Routes)
         {
            if (!MapControl.Markers.Contains(layerRoute))
            {
               MapControl.Markers.Add(layerRoute);
            }
         }
      }

      /// <summary>
      /// Is layer collapsed because of min\max zoom values
      /// </summary>
      public virtual bool HidenByZoomValidation
      {
         get
         {
            if (MapControl == null)
            {
               return true;
            }
            if (!AllowZoomLvlVisibilityValidation)
            {
               return false;
            }
            return MinLayerZoomLvl > MapControl.Zoom || MapControl.Zoom > MaxLayerZoomLvl;
         }
      }
      
      protected virtual void OnIsActiveChanged()
      {
         if (IsActive)
         {
            ShowLayerMarkers();
         }
         else
         {
            HideLayerMarkers();
         }
      }

      internal void HideLayerMarkers()
      {
         foreach (var mapMarker in Markers)
         {
            ProcessMarkerVisibility(mapMarker);
         }
      }

      internal void ShowLayerMarkers()
      {
         //todo: it can be too slow on large collections!
         foreach (var mapMarker in Markers)
         {
            ProcessMarkerVisibility(mapMarker);
            if (MapControl != null && !MapControl.Markers.Contains(mapMarker))
            {
               MapControl.Markers.Add(mapMarker);
            }
         }
      }

      private void ProcessMarkerVisibility(GMapMarker mapMarker)
      {
         if (mapMarker.Shape == null)
         {
            return;
         }

         if (IsActive && (!AllowZoomLvlVisibilityValidation || !HidenByZoomValidation || mapMarker.AllowZoomLvlVisibilityValidation == false))
         {
            mapMarker.Shape.Visibility = Visibility.Visible;
         }
         else
         {
            mapMarker.Shape.Visibility = Visibility.Collapsed;
         }
      }

      private void CreateEvents()
      {
         Markers.CollectionChanged += new NotifyCollectionChangedEventHandler(Markers_CollectionChanged);
         Routes.CollectionChanged += new NotifyCollectionChangedEventHandler(Routes_CollectionChanged);
         Polygons.CollectionChanged += new NotifyCollectionChangedEventHandler(Polygons_CollectionChanged);
      }

      private void Polygons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
      {
         throw new NotImplementedException();
      }

      private void Routes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
      {
         if (e.OldItems != null && MapControl != null)
         {
            foreach (GMapRoute item in e.OldItems)
            {
               MapControl.Markers.Remove(item);
            }
         }
         
         if (e.NewItems != null)
         {
            foreach (GMapRoute item in e.NewItems)
            {
               if (item.ZIndex == default(int))
               {
                  item.ZIndex = ZIndex;
               }
               ProcessMarkerVisibility(item);
               if (MapControl != null && !MapControl.Markers.Contains(item))
               {
                  MapControl.Markers.Add(item);
               }
            }
         }
      }

      private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
      {
         if (e.OldItems != null && MapControl != null)
         {
            foreach (GMapMarker item in e.OldItems)
            {
               MapControl.Markers.Remove(item);
            }
         }

         if (e.NewItems != null)
         {
            foreach (GMapMarker item in e.NewItems)
            {
               if (item.ZIndex == default(int))
               {
                  item.ZIndex = ZIndex;
               }

               if (item.AllowZoomLvlVisibilityValidation == null)
               {
                  item.AllowZoomLvlVisibilityValidation = AllowZoomLvlVisibilityValidation;
               }

               ProcessMarkerVisibility(item);
               if (MapControl != null && !MapControl.Markers.Contains(item))
               {
                  MapControl.Markers.Add(item);
               }
            }
         }
      }

      void ClearEvents()
      {
         Markers.CollectionChanged -= new NotifyCollectionChangedEventHandler(Markers_CollectionChanged);
         Routes.CollectionChanged -= new NotifyCollectionChangedEventHandler(Routes_CollectionChanged);
         Polygons.CollectionChanged -= new NotifyCollectionChangedEventHandler(Polygons_CollectionChanged);
      }

      #region ISerializable Members

      public void GetObjectData(SerializationInfo info, StreamingContext context)
      {
         throw new NotImplementedException();
      }

      #endregion

      #region IDeserializationCallback Members

      public void OnDeserialization(object sender)
      {
         throw new NotImplementedException();
      }

      #endregion

      #region IDisposable Members

      bool disposed = false;


      public void Dispose()
      {
         if (!disposed)
         {
            disposed = true;

            ClearEvents();

            foreach (var m in Markers)
            {
               m.Dispose();
            }

            foreach (var r in Routes)
            {
               r.Dispose();
            }

            //foreach (var p in Polygons)
            //{
            //   p.Dispose();
            //}

            //Clear();
         }
      }

      #endregion
   }
}
