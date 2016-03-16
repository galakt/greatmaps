using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GMap.NET.WindowsPresentation.GHeat
{
   /// <summary>
   /// Thrown when no weight handler is set and some function is called requiring a weight handler
   /// </summary>
   public class NoWeightHandler : Exception
   {
      public NoWeightHandler(String message) : base(message) { }
   }

   /// <summary>
   /// Manages the points to be applied to the tiles
   /// </summary>
   public class PointManager
   {
      List<PointLatLng> _pointList;
      private Projections.MercatorProjection _projection = new Projections.MercatorProjection();
      IWeightHandler _weightHandler;

      public PointManager() : this(null) { }

      /// <summary>
      /// Weight handle is used to get the weight for each point
      /// </summary>
      /// <param name="weightHandle"></param>
      public PointManager(IWeightHandler weightHandle)
      {
         _pointList = new List<PointLatLng>();
         _weightHandler = weightHandle;
      }

      /// <summary>
      /// Adds the point to the list. Also applies the weight to the point when their is point data
      /// </summary>
      /// <param name="point"></param>
      public void AddPoint(PointLatLng point)
      {
         //Apply the weight to the new point
         if (_weightHandler != null && point.Data != null) point.Weight = _weightHandler.Evaluate(point.Data);
         _pointList.Add(point);
      }

      /// <summary>
      /// Adds the points to the list. Also applies the weight to the point when their is point data
      /// </summary>
      /// <param name="points"></param>
      public void AddPoint(PointLatLng[] points)
      {
         //Apply the weight to the new point
         if (_weightHandler != null)
            for (int i = 0; i < points.Length; i++)
               if (points[i].Data != null) points[i].Weight = _weightHandler.Evaluate(points[i].Data);

         _pointList.AddRange(points);
      }

      /// <summary>
      /// Updates all of the weights for each point. Should not be called if their is no weight handlers
      /// </summary>
      public void UpdatePointWeights()
      {
         PointLatLng tempPoint;
         if (_weightHandler == null)
            throw new NoWeightHandler("Point weights can't be updated because a weight handler was not specified");
         else
            for (int i = 0; i < _pointList.Count; i++)
            {
               tempPoint = _pointList[i];
               if (tempPoint.Data != null) tempPoint.Weight = _weightHandler.Evaluate(tempPoint.Data);
               _pointList[i] = tempPoint;
            }
      }

      /// <summary>
      /// Gets the count of all of the points
      /// </summary>
      /// <returns></returns>
      public int PointCount()
      {
         return _pointList.Count;
      }

      /// <summary>
      /// Clears all of the points from the list
      /// </summary>
      public void ClearPointList()
      {
         _pointList.Clear();
      }

      /// <summary>
      /// Loads points from a file, just like in the Python GHeat app
      /// </summary>
      /// <param name="source"></param>
      /// <param name="weight">Force a weight on the point for testing</param>
      public void LoadPointsFromFile(string source, decimal weight)
      {
         string[] item;
         string[] lines = System.IO.File.ReadAllLines(source);
         foreach (string line in lines)
         {
            item = line.Split(',');
            var lat = double.Parse(item[1]);
            var ln = double.Parse(item[2]);
            _pointList.Add(new PointLatLng(double.Parse(item[1]), double.Parse(item[2]), null, weight));
         }
      }

      /// <summary>
      /// Loads points from a file, just like in the Python GHeat app
      /// </summary>
      /// <param name="source"></param>
      public void LoadPointsFromFile(string source)
      {
         string[] item;
         string[] lines = System.IO.File.ReadAllLines(source);
         foreach (string line in lines)
         {
            item = line.Split(',');
            var lat = double.Parse(item[1], new NumberFormatInfo() { CurrencyDecimalSeparator = "." });
            var ln = double.Parse(item[2], new NumberFormatInfo() { CurrencyDecimalSeparator = "." });

            _pointList.Add(new PointLatLng(lat, ln));
         }
      }

      /// <summary>
      /// Gets all of the points in the diameter specified
      /// </summary>
      /// <param name="center"></param>
      /// <param name="pixelsFromCenter">diameter</param>
      /// <param name="zoom">current zoom</param>
      /// <returns></returns>
      public GPoint[] GetPointsAroundCenter(PointLatLng center, int pixelsFromCenter, int zoom)
      {
         GPoint centerAsPixels;
         GPoint tlb;
         GPoint lrb;
         List<GPoint> points = new List<GPoint>();
         GSize max_lrb;
         GSize min_tlb;
         GPoint tempPoint;


         centerAsPixels = _projection.FromLatLngToPixel(center, zoom);
         min_tlb = _projection.GetTileMatrixMinXY(zoom);
         max_lrb = _projection.GetTileMatrixMaxXY(zoom);

         tlb = new GPoint(
                          centerAsPixels.X - pixelsFromCenter,
                          centerAsPixels.Y - pixelsFromCenter);

         lrb = new GPoint(
                        centerAsPixels.X + pixelsFromCenter,
                        centerAsPixels.Y + pixelsFromCenter);

         foreach (PointLatLng llPoint in GetList(tlb, lrb, zoom, false))
         {
            //Add the point to the list
            tempPoint = _projection.FromLatLngToPixel(llPoint.Lat, llPoint.Lng, zoom);
            tempPoint.Data = llPoint.Data;
            tempPoint.Weight = llPoint.Weight;
            points.Add(tempPoint);
         }
         return points.ToArray();
      }

      /// <summary>
      /// Gets all of the points that fit on the tile
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <param name="dot"></param>
      /// <param name="zoom"></param>
      /// <param name="newMethod"></param>
      /// <returns></returns>
      public GPoint[] GetPointsForTile(int x, int y, System.Drawing.Bitmap dot, int zoom, bool newMethod)
      {
         List<GPoint> points = new List<GPoint>();
         GSize maxTileSize;
         GPoint adjustedPoint;
         GPoint pixelCoordinate;
         GPoint mapPoint;

         maxTileSize = _projection.GetTileMatrixMaxXY(zoom);
         //Top Left Bounds
         GPoint tlb = _projection.FromTileXYToPixel(new GPoint(x, y));

         maxTileSize = new GSize(GHeat.SIZE, GHeat.SIZE);
         //Lower right bounds
         GPoint lrb = new GPoint((tlb.X + maxTileSize.Width) + dot.Width, (tlb.Y + maxTileSize.Height) + dot.Width);

         //pad the Top left bounds
         tlb = new GPoint(tlb.X - dot.Width, tlb.Y - dot.Height);


         //Go throught the list and convert the points to pixel cooridents
         foreach (PointLatLng llPoint in GetList(tlb, lrb, zoom, newMethod))
         {
            //Now go through the list and turn it into pixel points
            pixelCoordinate = _projection.FromLatLngToPixel(llPoint.Lat, llPoint.Lng, zoom);

            //Make sure the weight and data is still pointing after the conversion
            pixelCoordinate.Data = llPoint.Data;
            pixelCoordinate.Weight = llPoint.Weight;

            mapPoint = _projection.FromPixelToTileXY(pixelCoordinate);
            mapPoint.Data = pixelCoordinate.Data;

            //Adjust the point to the specific tile
            adjustedPoint = AdjustMapPixelsToTilePixels(new GPoint(x, y), pixelCoordinate);

            //Make sure the weight and data is still pointing after the conversion
            adjustedPoint.Data = pixelCoordinate.Data;
            adjustedPoint.Weight = pixelCoordinate.Weight;

            //Add the point to the list
            points.Add(adjustedPoint);
         }

         return points.ToArray();
      }

      /// <summary>
      /// Gets all of the points in the list (Lat Lng Format)
      /// </summary>
      /// <returns></returns>
      public PointLatLng[] GetAllPoints()
      {
         return _pointList.ToArray();
      }

      /// <summary>
      /// Gets a list of points that fit with in the Top Left Bounds and Lower Right Bounds
      /// </summary>
      /// <param name="tlb">Top Left Bounds</param>
      /// <param name="lrb">Lower Right Bounds</param>
      /// <param name="zoom"></param>
      /// <param name="newMethod"></param>
      /// <returns></returns>
      protected PointLatLng[] GetList(GPoint tlb, GPoint lrb, int zoom, bool newMethod)
      {
         List<GPoint> points = new List<GPoint>();
         IEnumerable<PointLatLng> llList;

         PointLatLng ptlb;
         PointLatLng plrb;

         ptlb = _projection.FromPixelToLatLng(tlb, zoom);
         plrb = _projection.FromPixelToLatLng(lrb, zoom);

         //Find all of the points that belong in the expanded tile
         // Some points may appear in more than one tile depending where they appear
         if (newMethod)
         {
            ListSearch ls = new ListSearch(_pointList, ptlb, plrb);
            llList = ls.GetMatchingPoints();
         }
         else
         {
            llList = from point in _pointList
                     where
                            point.Lat <= ptlb.Lat && point.Lng >= ptlb.Lng &&
                            point.Lat >= plrb.Lat && point.Lng <= plrb.Lng
                     select point;
         }
         return llList.ToArray();
      }

      /// <summary>
      /// Converts the whole map pixes to tile pixels
      /// </summary>
      /// <param name="tileXYPoint"></param>
      /// <param name="mapPixelPoint"></param>
      /// <returns></returns>
      public static GPoint AdjustMapPixelsToTilePixels(GPoint tileXYPoint, GPoint mapPixelPoint)
      {
         return new GPoint(mapPixelPoint.X - (tileXYPoint.X * GHeat.SIZE), mapPixelPoint.Y - (tileXYPoint.Y * GHeat.SIZE));
      }
   }
}
