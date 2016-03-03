using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GMap.NET.WindowsPresentation.HelpersAndUtils
{
   public enum ScaleModes
   {
      /// <summary>
      /// no scaling
      /// </summary>
      Integer,

      /// <summary>
      /// scales to fractional level using a stretched tiles, any issues -> http://greatmaps.codeplex.com/workitem/16046
      /// </summary>
      ScaleUp,

      /// <summary>
      /// scales to fractional level using a narrowed tiles, any issues -> http://greatmaps.codeplex.com/workitem/16046
      /// </summary>
      ScaleDown,

      /// <summary>
      /// scales to fractional level using a combination both stretched and narrowed tiles, any issues -> http://greatmaps.codeplex.com/workitem/16046
      /// </summary>
      Dynamic
   }
}
