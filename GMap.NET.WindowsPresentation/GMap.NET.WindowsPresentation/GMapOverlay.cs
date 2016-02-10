using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

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

      private bool _isVisible;

      public string OverlayId { get; set; }
      
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
      public bool IsVisible
      {
         get { return _isVisible; }
         set
         {
            _isVisible = value;
            OnIsVisibleChanged();
         }
      }

      public GMapControl MapControl { get; set; }
      
      private void OnIsVisibleChanged()
      {
         if (MapControl == null)
            return;
         //todo: delete or make invisible
         if (IsVisible)
         {
            foreach (var mapMarker in Markers)
            {
               MapControl.Markers.Add(mapMarker);
            }      
         }
         else
         {
            foreach (var mapMarker in Markers)
            {
               MapControl.Markers.Remove(mapMarker);
            }
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
         if (!IsVisible)
            return;
         if (MapControl == null)
            return;

         foreach (GMapMarker item in e.OldItems)
         {
            MapControl.Markers.Remove(item);
         };
         foreach (GMapMarker item in e.NewItems)
         {
            MapControl.Markers.Add(item);
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
