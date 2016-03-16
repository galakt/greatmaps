using System;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

namespace GMap.NET.GMap.NET.MapProviders.GHeat
{
   public class GHeatMapProvider : GMapProvider
   {
      public GHeatMapProvider()
      {
      }

      #region GMapProvider Members

      public override Guid Id { get; } = new Guid("36DF95BB-AA6F-4FB4-B732-9ADDA894DA5E");
      public override string Name { get; } = "GHeatMap";
      public override PureProjection Projection { get; } = MercatorProjectionYandex.Instance;
      public override GMapProvider[] Overlays { get; }
      public override PureImage GetTileImage(GPoint pos, int zoom)
      {
         throw new NotImplementedException();
      }

      #endregion GMapProvider Members
   }
}
