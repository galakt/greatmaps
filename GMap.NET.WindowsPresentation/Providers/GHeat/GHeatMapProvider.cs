using System;
using System.Collections.Generic;
using System.Drawing;
using GMap.NET.GHeat;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

namespace GMap.NET.WindowsPresentation.Providers.GHeat
{
   public class GHeatMapProvider : GMapProvider
   {
      private readonly IEnumerable<PointLatLng> _points;
      private readonly PointManager _pointManager = new PointManager();

      public GHeatMapProvider(IEnumerable<PointLatLng> points)
      {
         _points = points;
      }

      #region GMapProvider Members

      public override Guid Id { get; } = new Guid("36DF95BB-AA6F-4FB4-B732-9ADDA894DA5E");
      public override string Name { get; } = "GHeatMap";
      public override PureProjection Projection { get; } = MercatorProjection.Instance;
      GMapProvider[] overlays;
      public override GMapProvider[] Overlays => overlays ?? (overlays = new GMapProvider[] {this});

      public override PureImage GetTileImage(GPoint pos, int zoom)
      {
         Bitmap tempImage = GMap.NET.GHeat.GHeat.GetTile(_pointManager, SchemeNames.Classic, zoom, (int)pos.X, (int)pos.Y, false, false, 150);
         ImageConverter ic = new ImageConverter();
         var ba = (byte[])ic.ConvertTo(tempImage, typeof(byte[]));
         return GMapImageProxy.Instance.FromArray(ba);
      }

      #endregion GMapProvider Members
   }
}
