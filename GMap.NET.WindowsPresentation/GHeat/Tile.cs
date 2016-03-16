using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace GMap.NET.WindowsPresentation.GHeat
{
   /// <summary>
   /// Gets a single tile
   /// NOTE: Blank tiles are cached so don't dispose them
   /// </summary>
   public class Tile
   {

      private static Dictionary<string, Bitmap> _emptyTile = new Dictionary<string, Bitmap>();
      private static object _emptyTileLocker = new Object();
      private static int[] _zoomOpacity = null;
      private static Tile _thisInstance = new Tile();
      private static Stopwatch sw = new Stopwatch();

      private Tile()
      {
         Opacity o = new Opacity();
         _zoomOpacity = o.BuildZoomMapping();
      }

      /// <summary>
      /// Generates a tile
      /// 
      /// For blank Tiles:
      /// You must NOT modify the image. Don't Dispose it!!! It is cached, so if you dispose it you have nothing!!
      /// </summary>
      /// <param name="colorScheme">Image Color scheme</param>
      /// <param name="dot">Image dot</param>
      /// <param name="zoom">Current zoom level</param>
      /// <param name="tileX">Tile x coordinante</param>
      /// <param name="tileY">Tile y coordinante</param>
      /// <param name="points">Points to add</param>
      /// <param name="changeOpacityWithZoom">If false the default opacity is used instead of a changing value</param>
      /// <param name="defaultOpacity">Used when change opacity with zoom is false</param>
      /// <returns></returns>
      public static Bitmap Generate(Bitmap colorScheme, Bitmap dot, int zoom, int tileX, int tileY, GMap.NET.GPoint[] points, bool changeOpacityWithZoom, int defaultOpacity)
      {
         int expandedWidth;
         int expandedHeight;
         int dotHalfSize;
         int pad;

         int x1;
         int x2;
         int y1;
         int y2;

         if (defaultOpacity < Opacity.TRANSPARENT || defaultOpacity > Opacity.OPAQUE)
            throw new Exception("The default opacity of '" + defaultOpacity.ToString() + "' doesn't fall between '" + Opacity.TRANSPARENT.ToString() + "' and '" + Opacity.OPAQUE.ToString() + "'");

         //Translate tile to pixel coords.
         x1 = tileX * GHeat.SIZE;
         x2 = x1 + 255;
         y1 = tileY * GHeat.SIZE;
         y2 = y1 + 255;

         dotHalfSize = dot.Width;
         pad = dotHalfSize;
         int extraPad = dot.Width * 2;

         //Expand bounds by one dot width.
         x1 = x1 - extraPad;
         x2 = x2 + extraPad;
         y1 = y1 - extraPad;
         y2 = y2 + extraPad;
         expandedWidth = x2 - x1;
         expandedHeight = y2 - y1;

         Bitmap tile;
         if (points.Length == 0)
         {
            if (changeOpacityWithZoom)
               tile = GetEmptyTile(colorScheme, _zoomOpacity[zoom]);
            else
               tile = GetEmptyTile(colorScheme, defaultOpacity);
         }
         else
         {
            sw.Start();
            tile = GetBlankImage(expandedHeight, expandedWidth);
            sw.Stop();
            Debug.WriteLine($"GetBlankImage = {sw.ElapsedMilliseconds} ms");
            sw.Reset();
            sw.Start();
            tile = AddPoints(tile, dot, points);
            sw.Stop();
            Debug.WriteLine($"AddPoints = {sw.ElapsedMilliseconds} ms");
            tile = Trim(tile, dot);
            sw.Reset();
            sw.Start();
            if (changeOpacityWithZoom)
               tile = Colorize(tile, colorScheme, _zoomOpacity[zoom]);
            else
               tile = Colorize(tile, colorScheme, defaultOpacity);
            sw.Stop();
            Debug.WriteLine($"Colorize = {sw.ElapsedMilliseconds} ms");
         }
         return tile;
      }

      /// <summary>
      /// Takes the gray scale and applies the color scheme to it.
      /// </summary>
      /// <param name="tile"></param>
      /// <returns></returns>
      public static Bitmap Colorize(Bitmap tile, Bitmap colorScheme, int zoomOpacity)
      {
         Color tilePixelColor;
         Color colorSchemePixel;

         for (int x = 0; x < tile.Width; x++)
         {
            for (int y = 0; y < tile.Height; y++)
            {
               //Get color for this intensity
               tilePixelColor = tile.GetPixel(x, y);

               //Get the color of the scheme from the intensity on the tile
               //Only need to get one color in the tile, because it is grayscale, so each color will have the same intensity
               colorSchemePixel = colorScheme.GetPixel(0, tilePixelColor.R);

               zoomOpacity = (int)(
                   (
                       ((float)zoomOpacity / 255.0f)
                       *
                       ((float)colorSchemePixel.A / 255.0f)
                   ) * 255f
                   );
               tile.SetPixel(x, y, Color.FromArgb(zoomOpacity, colorSchemePixel));
            }
         }
         return tile;
      }

      /// <summary>
      /// Trim the larger tile to the correct size
      /// </summary>
      /// <param name="tile"></param>
      /// <returns></returns>
      public static Bitmap Trim(Bitmap tile, Bitmap dot)
      {
         Bitmap croppedTile = new Bitmap(GHeat.SIZE, GHeat.SIZE, PixelFormat.Format32bppArgb);
         Graphics g = Graphics.FromImage(croppedTile);
         int adjPad = dot.Width + (dot.Width / 2);

         g.DrawImage(
                     tile, // Source Image
                     new System.Drawing.Rectangle(0, 0, GHeat.SIZE, GHeat.SIZE),
                     adjPad, // source x, adjusted for padded amount
                     adjPad, // source y, adjusted for padded amount
                     GHeat.SIZE, //source width
                     GHeat.SIZE, // source height
                     GraphicsUnit.Pixel
                     );
         return croppedTile;
      }

      /// <summary>
      /// Add all of the points to the tile
      /// </summary>
      /// <param name="tile"></param>
      /// <param name="points"></param>
      /// <returns></returns>
      public static Bitmap AddPoints(Bitmap tile, Bitmap dot, GMap.NET.GPoint[] points)
      {
         ImageBlender blender = new ImageBlender();

         for (int i = 0; i < points.Length; i++)
         {
            if (points[i].Weight.HasValue && points[i].Weight.Value == 0)
            {
               //Skip a value of zero, it scored so low it should not show up.
            }
            else
            {
               //Blend each dot to the existing images
               blender.BlendImages(
                           tile, // Destination Image
                           (int)points[i].X + dot.Width, //Dest x
                           (int)points[i].Y + dot.Width, //Dest y
                           dot.Width, // Dest width
                           dot.Height,  // Dest height
                                        //If their is a weight then change the dot so it reflects that intensity
                           points[i].Weight.HasValue ? ApplyWeightToImage(dot, points[i].Weight.Value) : dot, // Src Image
                           0, // Src x
                           0, // Src y
                           ImageBlender.BlendOperation.Blend_Multiply);
            }
         }
         return tile;
      }

      /// <summary>
      /// Change the intensity of the image
      /// </summary>
      /// <param name="image">Dot image</param>
      /// <param name="weight">Weight to apply</param>
      /// <returns></returns>
      private static Bitmap ApplyWeightToImage(Bitmap image, decimal weight)
      {
         Graphics graphic;
         float tempWeight;
         Bitmap tempImage = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);

         graphic = Graphics.FromImage(tempImage);

         //I want to make the color more intense (White/bright)

         tempWeight = (float)(weight);
         // ColorMatrix elements
         float[][] ptsArray =
             {
                new float[] {tempWeight, 0, 0, 0, 0},
                new float[] {0, tempWeight, 0, 0, 0},
                new float[] {0, 0,tempWeight, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
                };


         // Create a ColorMatrix
         ColorMatrix clrMatrix = new ColorMatrix(ptsArray);
         // Create ImageAttributes
         ImageAttributes imgAttribs = new ImageAttributes();
         // Set color matrix
         //imgAttribs.SetColorMatrix(clrMatrix,
         //            //I dont know why, but when i tell it to Skip the Grays the color altering
         //            // works on all computers. Otherwise it does not shade correctly.
         //            ColorMatrixFlag.SkipGrays,
         //            ColorAdjustType.Bitmap);
         //http://www.c-sharpcorner.com/UploadFile/puranindia/610/
         //Gamma values range from 0.1 to 5.0 (normally 0.1 to 2.2), with 0.1 being the brightest and 5.0 the darkest.
         //Convert the 100% to a range of 0-5 by multiplying it by 5
         imgAttribs.SetGamma((tempWeight == 0 ? .1f : (tempWeight * 5)), ColorAdjustType.Bitmap);

         // Draw Image with the attributes
         graphic.DrawImage(image,
                         new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                         0, 0, image.Width, image.Height,
                         GraphicsUnit.Pixel, imgAttribs);

         //New dot with a different intensity
         return tempImage;
      }

      /// <summary>
      /// Gets a blank image / canvas
      /// </summary>
      /// <returns></returns>
      public static Bitmap GetBlankImage(int height, int width)
      {
         Bitmap newImage;
         Graphics g;

         //Create a blank tile that is 32 bit and has an alpha
         newImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
         g = Graphics.FromImage(newImage);
         //Background must be white so the dots can blend
         g.FillRectangle(Brushes.White, new System.Drawing.Rectangle(0, 0, width, height));
         return newImage;
      }

      /// <summary>
      /// Empty tile with no points on it.
      /// NOTE: You must not modify this image. Don't Dispose it!!!
      /// </summary>
      /// <returns></returns>
      public static Bitmap GetEmptyTile(Bitmap colorScheme, int zoomOpacity)
      {
         Color colorSchemePixelColor;
         Bitmap tile;
         Graphics graphic;
         SolidBrush solidBrush;

         //If we have already created the empty tile then return it
         if (_emptyTile.ContainsKey(colorScheme.GetHashCode().ToString() + "_" + zoomOpacity.ToString()))
            return _emptyTile[colorScheme.GetHashCode() + "_" + zoomOpacity.ToString()];

         //Create a blank tile that is 32 bit and has an alpha
         tile = new Bitmap(GHeat.SIZE, GHeat.SIZE, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

         graphic = Graphics.FromImage(tile);

         //Get the first pixel of the color scheme, on the dark side 
         colorSchemePixelColor = colorScheme.GetPixel(0, colorScheme.Height - 1);

         zoomOpacity = (int)((
                         (zoomOpacity / 255.0f) //# From Configuration
                         *
                         (colorSchemePixelColor.A / 255.0f) //#From per-pixel alpha
                         ) * 255.0f);

         solidBrush = new SolidBrush(Color.FromArgb(zoomOpacity, colorSchemePixelColor.R, colorSchemePixelColor.G, colorSchemePixelColor.B));
         graphic.FillRectangle(solidBrush, 0, 0, GHeat.SIZE, GHeat.SIZE);
         graphic.Dispose();

         //Save the newly created empty tile
         //There is a empty tile for each scheme and zoom level
         lock (_emptyTileLocker)
         {
            //Double check it does not already exists
            if (!_emptyTile.ContainsKey(colorScheme.GetHashCode().ToString()))
               _emptyTile.Add(colorScheme.GetHashCode().ToString(), tile);
         }
         return tile;
      }
   }
}
