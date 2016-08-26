using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace GMap.NET.WindowsPresentation.HelpersAndUtils
{
   internal static class ScreenCopyHelper
   {
      internal static BitmapSource CopyScreen()
      {
         using (var screenBmp = new Bitmap(
            (int) SystemParameters.PrimaryScreenWidth,
            (int) SystemParameters.PrimaryScreenHeight, PixelFormat.Format32bppArgb))
         {
            using (var bmpGraphics = Graphics.FromImage(screenBmp))
            {
               bmpGraphics.CopyFromScreen(0, 0, 0, 0, screenBmp.Size);
               return Imaging.CreateBitmapSourceFromHBitmap(
                  screenBmp.GetHbitmap(),
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  BitmapSizeOptions.FromEmptyOptions());
            }
         }
      }

      internal static BitmapSource CopyScreen(int left, int top, int right, int bottom)
      {
         using (var screenBmp = new Bitmap(right - left, bottom - top, PixelFormat.Format32bppArgb))
         {
            using (var bmpGraphics = Graphics.FromImage(screenBmp))
            {
               bmpGraphics.CopyFromScreen(left, top, right, bottom, screenBmp.Size);
               return Imaging.CreateBitmapSourceFromHBitmap(
                  screenBmp.GetHbitmap(),
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  BitmapSizeOptions.FromEmptyOptions());
            }
         }
      }

      internal static Bitmap CopyScreenToBitmap(int left, int top, int right, int bottom)
      {
         var screenBmp = new Bitmap(right - left, bottom - top, PixelFormat.Format32bppArgb);
         using (var bmpGraphics = Graphics.FromImage(screenBmp))
         {
            bmpGraphics.CopyFromScreen(left, top, right, bottom, screenBmp.Size);
            return screenBmp;
         }
      }

      internal static void PrintMap(int width, int height, Visual control)
      {
         Debug.WriteLine($"PrintMap=> width={width} height={height}");
         if (width < 0)
         {
            width = 100;
         }
         if (height < 0)
         {
            height = 100;
         }
         RenderTargetBitmap renderTargetBitmap =
            new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
         renderTargetBitmap.Render(control);
         PngBitmapEncoder pngImage = new PngBitmapEncoder();
         pngImage.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
         using (Stream fileStream = File.Create(@"D:\tmp\1.jpg"))
         {
            pngImage.Save(fileStream);
         }
      }

      //internal static BitmapSource CopyScreen(int width, int height)
      //{
      //   using (var screenBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
      //   {
      //      using (var bmpGraphics = Graphics.FromImage(screenBmp))
      //      {
      //         bmpGraphics.CopyFromScreen(left, top, right, bottom, screenBmp.Size);
      //         return Imaging.CreateBitmapSourceFromHBitmap(
      //             screenBmp.GetHbitmap(),
      //             IntPtr.Zero,
      //             Int32Rect.Empty,
      //             BitmapSizeOptions.FromEmptyOptions());
      //      }
      //   }
      //}
   }
}
