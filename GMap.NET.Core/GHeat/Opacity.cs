using System.Collections.Generic;

namespace GMap.NET.GHeat
{
   /// <summary>
   /// Creates a zoom map
   /// zoom # = Opacity of image
   /// </summary>
   public class Opacity
   {
      /// <summary>
      /// Alpha value that indicates an image is not transparent
      /// </summary>
      public const int OPAQUE = 255;
      /// <summary>
      /// Alpha value that indicates that an image is transparent
      /// </summary>
      public const int TRANSPARENT = 0;
      /// <summary>
      /// Max zoom that google supports
      /// </summary>
      public const int MAX_ZOOM = 31;
      public const int ZOOM_OPAQUE = -15;
      public const int ZOOM_TRANSPARENT = 15;
      public const int DEFAULT_OPACITY = 50;

      private int _zoomOpaque;
      private int _zoomTransparent;

      public Opacity(int zoomOpaque, int zoomTransparent)
      {
         _zoomOpaque = zoomOpaque;
         _zoomTransparent = zoomTransparent;
      }

      /// <summary>
      /// Uses default values if not specified
      /// </summary>
      public Opacity()
      {
         _zoomOpaque = ZOOM_OPAQUE;
         _zoomTransparent = ZOOM_TRANSPARENT;
      }

      /// <summary>
      /// Build and return the zoom_to_opacity mapping
      /// </summary>
      /// <returns>index=zoom and value of the element is the opacity</returns>
      public int[] BuildZoomMapping()
      {
         List<int> zoomMapping = new List<int>();
         int numberOfOpacitySteps;
         float opacityStep;

         numberOfOpacitySteps = _zoomTransparent - _zoomOpaque;

         if (numberOfOpacitySteps < 1) //don't want general fade
         {
            for (int i = 0; i <= MAX_ZOOM; i++)
               zoomMapping.Add(0);
         }
         else //want general fade
         {
            opacityStep = ((float)OPAQUE / (float)numberOfOpacitySteps); //chunk of opacity
            for (int zoom = 0; zoom <= MAX_ZOOM; zoom++)
            {
               if (zoom <= _zoomOpaque)
                  zoomMapping.Add(OPAQUE);
               else if (zoom >= _zoomTransparent)
                  zoomMapping.Add(TRANSPARENT);
               else
                  zoomMapping.Add((int)((float)OPAQUE - (((float)zoom - (float)_zoomOpaque) * opacityStep)));
            }
         }
         return zoomMapping.ToArray();
      }
   }
}
