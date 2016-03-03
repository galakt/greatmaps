using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace GMap.NET.WindowsPresentation.HelpersAndUtils
{
   public static class PathHelper
   {
      /// <summary>
      /// Creates path from list of points, for performance set addBlurEffect to false
      /// </summary>
      /// <returns></returns>
      public static Path CreateRoutePath(List<Point> localPath, bool addBlurEffect = false)
      {
         // Create a StreamGeometry to use to specify myPath.
         StreamGeometry geometry = new StreamGeometry();
         using (StreamGeometryContext ctx = geometry.Open())
         {
            ctx.BeginFigure(localPath[0], false, false);
            // Draw a line to the next specified point.
            ctx.PolyLineTo(localPath, true, true);
         }

         // Freeze the geometry (make it unmodifiable)
         // for additional performance benefits.
         geometry.Freeze();

         // Create a path to draw a geometry with.
         var path = new Path();
         {
            // Specify the shape of the Path using the StreamGeometry.
            path.Data = geometry;
            if (addBlurEffect)
            {
               var blurEffect = new BlurEffect
               {
                  KernelType = KernelType.Gaussian,
                  Radius = 3.0,
                  RenderingBias = RenderingBias.Performance
               };
               path.Effect = blurEffect;
            }

            path.Stroke = Brushes.Navy;
            if(path.Stroke.CanFreeze) { path.Stroke.Freeze(); }
            path.StrokeThickness = 5;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.StrokeStartLineCap = PenLineCap.Triangle;
            path.StrokeEndLineCap = PenLineCap.Square;
            path.Opacity = 0.6;
            path.IsHitTestVisible = false;
         }
         return path;
      }
      
      /// <summary>
      /// Creates path from list of points, for performance set addBlurEffect to false
      /// </summary>
      /// <returns></returns>
      public static Path CreatePolygonPath(List<Point> localPath, bool addBlurEffect = false)
      {
         // Create a StreamGeometry to use to specify myPath.
         StreamGeometry geometry = new StreamGeometry();
         using (StreamGeometryContext ctx = geometry.Open())
         {
            ctx.BeginFigure(localPath[0], true, true);
            // Draw a line to the next specified point.
            ctx.PolyLineTo(localPath, true, true);
         }
         // Freeze the geometry (make it unmodifiable)
         // for additional performance benefits.
         geometry.Freeze();
         // Create a path to draw a geometry with.
         var path = new Path();
         {
            // Specify the shape of the Path using the StreamGeometry.
            path.Data = geometry;
            if (addBlurEffect)
            {
               var blurEffect = new BlurEffect
               {
                  KernelType = KernelType.Gaussian,
                  Radius = 3.0,
                  RenderingBias = RenderingBias.Performance,
               };
               path.Effect = blurEffect;
            }

            path.Stroke = Brushes.MidnightBlue;
            if(path.Stroke.CanFreeze) { path.Stroke.Freeze(); }
            path.StrokeThickness = 5;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.StrokeStartLineCap = PenLineCap.Triangle;
            path.StrokeEndLineCap = PenLineCap.Square;
            path.Fill = Brushes.AliceBlue;
            if(path.Fill.CanFreeze) { path.Fill.Freeze(); }
            path.Opacity = 0.6;
            path.IsHitTestVisible = false;
         }
         return path;
      }
   }
}
