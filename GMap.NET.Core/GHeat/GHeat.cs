using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

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
      public const string DOTS_FOLDER = "Dots";
      /// <summary>
      /// Color scheme folder name
      /// </summary>
      public const string COLOR_SCHMES_FOLDER = "ColorSchemes";

      /// <summary>
      /// Contains a cache of dot images
      /// </summary>
      private static Dictionary<string, Bitmap> dotsList;

      /// <summary>
      /// Contains a cache of color schemes
      /// </summary>
      private static Dictionary<string, Bitmap> colorSchemeList;

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
         //string directory = Settings.BaseDirectory;
         dotsList = new Dictionary<string, Bitmap>();
         colorSchemeList = new Dictionary<string, Bitmap>();

         //foreach (string file in System.IO.Directory.GetFiles(directory + DOTS_FOLDER, "*." + ImageFormat.Png.ToString().ToLower()))
         //   dotsList.Add(Path.GetFileName(file), new Bitmap(file));







         dotsList.Add("dot0.png", Properties.Resources.dot0);
         dotsList.Add("dot1.png", Properties.Resources.dot1);
         dotsList.Add("dot2.png", Properties.Resources.dot2);
         dotsList.Add("dot3.png", Properties.Resources.dot3);
         dotsList.Add("dot4.png", Properties.Resources.dot4);
         dotsList.Add("dot5.png", Properties.Resources.dot5);
         dotsList.Add("dot6.png", Properties.Resources.dot6);
         dotsList.Add("dot7.png", Properties.Resources.dot7);
         dotsList.Add("dot8.png", Properties.Resources.dot8);
         dotsList.Add("dot9.png", Properties.Resources.dot9);
         dotsList.Add("dot10.png", Properties.Resources.dot10);
         dotsList.Add("dot11.png", Properties.Resources.dot11);
         dotsList.Add("dot12.png", Properties.Resources.dot12);
         dotsList.Add("dot13.png", Properties.Resources.dot13);
         dotsList.Add("dot14.png", Properties.Resources.dot14);
         dotsList.Add("dot15.png", Properties.Resources.dot15);
         dotsList.Add("dot16.png", Properties.Resources.dot16);
         dotsList.Add("dot17.png", Properties.Resources.dot17);
         dotsList.Add("dot18.png", Properties.Resources.dot18);
         dotsList.Add("dot19.png", Properties.Resources.dot19);
         dotsList.Add("dot20.png", Properties.Resources.dot20);
         dotsList.Add("dot21.png", Properties.Resources.dot21);
         dotsList.Add("dot22.png", Properties.Resources.dot22);
         dotsList.Add("dot23.png", Properties.Resources.dot23);
         dotsList.Add("dot24.png", Properties.Resources.dot24);
         dotsList.Add("dot25.png", Properties.Resources.dot25);
         dotsList.Add("dot26.png", Properties.Resources.dot26);
         dotsList.Add("dot27.png", Properties.Resources.dot27);
         dotsList.Add("dot28.png", Properties.Resources.dot28);
         dotsList.Add("dot29.png", Properties.Resources.dot29);
         dotsList.Add("dot30.png", Properties.Resources.dot30);


         colorSchemeList.Add("classic.png", Properties.Resources.classic);
         //var ff = Properties.Resources.CreateTileDb//Properties.Resources.ResourceManager.
         //foreach (string file in System.IO.Directory.GetFiles(directory + COLOR_SCHMES_FOLDER, "*." + ImageFormat.Png.ToString().ToLower()))
         //   colorSchemeList.Add(Path.GetFileName(file), new Bitmap(file));
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
      public static Bitmap GetTile(PointManager pm, string colorScheme, int zoom, int x, int y, bool newMethod, bool changeOpacityWithZoom, int defaultOpacity)
      {
         //Do a little error checking
         if (colorScheme == string.Empty) throw new Exception("A color scheme is required");
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
      public static Bitmap GetTile(PointManager pm, string colorScheme, int zoom, int x, int y)
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
         var dot = dotsList["dot" + zoom.ToString() + "." + ImageFormat.Png.ToString().ToLower()];
         lock (dot)
         {
            return (Bitmap)dotsList["dot" + zoom.ToString() + "." + ImageFormat.Png.ToString().ToLower()].Clone();
         }
      }

      /// <summary>
      /// Gets the color scheme from the cache
      /// </summary>
      /// <param name="schemeName"></param>
      /// <returns></returns>
      public static Bitmap GetColorScheme(string schemeName)
      {
         if (!colorSchemeList.ContainsKey(schemeName + "." + ImageFormat.Png.ToString().ToLower()))
            throw new Exception("Color scheme '" + schemeName + " could not be found");
         var scheme = colorSchemeList[schemeName + "." + ImageFormat.Png.ToString().ToLower()];
         lock (scheme)
         {
            return (Bitmap)colorSchemeList[schemeName + "." + ImageFormat.Png.ToString().ToLower()].Clone();
         }
      }

      public static string[] AvailableColorSchemes()
      {
         List<string> colorSchemes = new List<string>();

         //I dont want to return the file extention just the name
         foreach (string key in colorSchemeList.Keys)
            colorSchemes.Add(key.Replace("." + ImageFormat.Png.ToString().ToLower(), ""));
         return colorSchemes.ToArray();
      }
   }
}
