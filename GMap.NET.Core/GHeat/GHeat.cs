using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace GMap.NET.GHeat
{
   /// <summary>
   /// The base directory is set in the settings.
   /// </summary>
   public class GHeat
   {
      public const int SIZE = 256; // # size of (square) tile; NB: changing this will break gmerc calls!
      public const int MAX_ZOOM = 31; // # this depends on Google API; 0 is furthest out as of recent ver.
                                      /// <summary>
                                      /// Dots folder
                                      /// </summary>
      /// <summary>
      /// Contains a cache of dot images
      /// </summary>
      private static Dictionary<string, Bitmap> _dotsList;

      /// <summary>
      /// Contains a cache of color schemes
      /// </summary>
      private static Dictionary<SchemeNames, Bitmap> _colorSchemeDictionary;

      private static GHeat _nonWebInstance = new GHeat();
      /// <summary>
      /// Used for locking to ensure multi thread capability
      /// </summary>
      private static Object _instanceLocker = new Object();

      /// <summary>
      /// Loads all of the color schemes and dots into the cache
      /// </summary>
      private GHeat()
      {
         _dotsList = new Dictionary<string, Bitmap>();
         _colorSchemeDictionary = new Dictionary<SchemeNames, Bitmap>();

         _dotsList.Add("dot0", Properties.Resources.dot0);
         _dotsList.Add("dot1", Properties.Resources.dot1);
         _dotsList.Add("dot2", Properties.Resources.dot2);
         _dotsList.Add("dot3", Properties.Resources.dot3);
         _dotsList.Add("dot4", Properties.Resources.dot4);
         _dotsList.Add("dot5", Properties.Resources.dot5);
         _dotsList.Add("dot6", Properties.Resources.dot6);
         _dotsList.Add("dot7", Properties.Resources.dot7);
         _dotsList.Add("dot8", Properties.Resources.dot8);
         _dotsList.Add("dot9", Properties.Resources.dot9);
         _dotsList.Add("dot10", Properties.Resources.dot10);
         _dotsList.Add("dot11", Properties.Resources.dot11);
         _dotsList.Add("dot12", Properties.Resources.dot12);
         _dotsList.Add("dot13", Properties.Resources.dot13);
         _dotsList.Add("dot14", Properties.Resources.dot14);
         _dotsList.Add("dot15", Properties.Resources.dot15);
         _dotsList.Add("dot16", Properties.Resources.dot16);
         _dotsList.Add("dot17", Properties.Resources.dot17);
         _dotsList.Add("dot18", Properties.Resources.dot18);
         _dotsList.Add("dot19", Properties.Resources.dot19);
         _dotsList.Add("dot20", Properties.Resources.dot20);
         _dotsList.Add("dot21", Properties.Resources.dot21);
         _dotsList.Add("dot22", Properties.Resources.dot22);
         _dotsList.Add("dot23", Properties.Resources.dot23);
         _dotsList.Add("dot24", Properties.Resources.dot24);
         _dotsList.Add("dot25", Properties.Resources.dot25);
         _dotsList.Add("dot26", Properties.Resources.dot26);
         _dotsList.Add("dot27", Properties.Resources.dot27);
         _dotsList.Add("dot28", Properties.Resources.dot28);
         _dotsList.Add("dot29", Properties.Resources.dot29);
         _dotsList.Add("dot30", Properties.Resources.dot30);


         _colorSchemeDictionary.Add(SchemeNames.Classic, Properties.Resources.classic);
      }

      /// <summary>
      /// Gets a single tile
      /// </summary>
      /// <param name="pm"></param>
      /// <param name="colorScheme"></param>
      /// <param name="zoom"></param>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <param name="newMethod">An attempt at a divide an conquer via threading. Turned out to be slower than just a row scan</param>
      /// <param name="changeOpacityWithZoom"></param>
      /// <param name="defaultOpacity"> Default opacity when changeOpacityWithZoom is false</param>
      /// <returns></returns>
      public static Bitmap GetTile(PointManager pm, SchemeNames colorScheme, int zoom, int x, int y, bool newMethod, bool changeOpacityWithZoom, int defaultOpacity)
      {
         //Do a little error checking
         if (pm == null) throw new Exception("No point manager has been specified");
         return Tile.Generate(GetColorScheme(colorScheme), GetDot(zoom), zoom, x, y, pm.GetPointsForTile(x, y, GetDot(zoom), zoom, newMethod), changeOpacityWithZoom, defaultOpacity);
      }
      /// <summary>
      /// Gets a single tile
      /// Default method that matches the output of the python gheat
      /// </summary>
      /// <param name="pm"></param>
      /// <param name="colorScheme"></param>
      /// <param name="zoom"></param>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <returns></returns>
      public static Bitmap GetTile(PointManager pm, SchemeNames colorScheme, int zoom, int x, int y)
      {
         return GetTile(pm, colorScheme, zoom, x, y, false, true, 0);
      }

      /// <summary>
      /// Gets a dot from the cache
      /// </summary>
      /// <param name="zoom"></param>
      /// <returns></returns>
      private static Bitmap GetDot(int zoom)
      {
         var dot = _dotsList["dot" + zoom];
         lock (dot)
         {
            return (Bitmap)dot.Clone();
         }
      }

      /// <summary>
      /// Gets the color scheme from the cache
      /// </summary>
      /// <param name="schemeName"></param>
      /// <returns></returns>
      public static Bitmap GetColorScheme(SchemeNames schemeName)
      {
         if (!_colorSchemeDictionary.ContainsKey(schemeName))
            throw new Exception("Color scheme '" + schemeName + " could not be found");
         var scheme = _colorSchemeDictionary[schemeName];
         lock (scheme)
         {
            return (Bitmap)scheme.Clone();
         }
      }

      public static SchemeNames[] AvailableColorSchemes()
      {
         return _colorSchemeDictionary.Keys.ToArray();
      }
   }
}
