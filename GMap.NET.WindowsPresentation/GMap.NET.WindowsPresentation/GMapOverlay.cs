using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;

namespace GMap.NET.WindowsPresentation
{
   [Serializable]
   public class GMapOverlay : ISerializable, IDeserializationCallback, IDisposable
   {
      public GMapOverlay()
      {
         CreateEvents();
      }

      public GMapOverlay(string overlayId)
      {
         OverlayId = overlayId;
         CreateEvents();
      }

      private bool _isActive;

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
      public bool AllowChangeVisibilityOnZoom { get; set; }
      public int MaxLayerZoomLvl { get; set; }
      public int MinLayerZoomLvl { get; set; }

      internal bool HidenByZoomValidation
      {
         get
         {
            if (!AllowChangeVisibilityOnZoom)
               return false;
            return MinLayerZoomLvl > MapControl.Zoom || MapControl.Zoom > MaxLayerZoomLvl;
         }
      }

      internal GMapControl MapControl { get; set; }
      
      private void OnIsActiveChanged()
      {
         if (MapControl == null)
            return;

         if (IsActive)
         {
            ShowLayerMarkers();
         }
         else
         {
            HideLayerMarkers();
         }
      }

      //todo: delete or make invisible
      internal void HideLayerMarkers()
      {
         foreach (var mapMarker in Markers)
         {
            MapControl.Markers.Remove(mapMarker);
         }
      }

      internal void ShowLayerMarkers()
      {
         //todo: it can be too slow on large collections!
         foreach (var mapMarker in Markers)
         {
            if (!MapControl.Markers.Contains(mapMarker))
               MapControl.Markers.Add(mapMarker);
         }
      }

      void CreateEvents()
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
         throw new NotImplementedException();
      }

      private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
      {
         if (!IsActive)
            return;
         if (MapControl == null)
            return;

         if (e.OldItems != null)
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
               MapControl.Markers.Add(item);
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

            //foreach (var m in Markers)
            //{
            //   m.Dispose();
            //}

            //foreach (var r in Routes)
            //{
            //   r.Dispose();
            //}

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
