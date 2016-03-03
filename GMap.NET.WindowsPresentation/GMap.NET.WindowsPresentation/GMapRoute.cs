using GMap.NET.WindowsPresentation.Interfaces;
using System.Collections.Generic;
using System.Windows.Shapes;
using GMap.NET.WindowsPresentation.HelpersAndUtils;

namespace GMap.NET.WindowsPresentation
{
    public class GMapRoute : GMapMarker, IShapable
    {
        public readonly List<PointLatLng> Points = new List<PointLatLng>();

        public GMapRoute(IEnumerable<PointLatLng> points)
        {
            Points.AddRange(points);
        }
        
        public override void Clear()
        {
            base.Clear();
            Points.Clear();
        }

        /// <summary>
        /// Regenerates shape of route
        /// </summary>
        public virtual void RegenerateShape(GMapControl map)
        {
            if (map != null)
            {
                this.Map = map;

                if(Points.Count > 1)
                {
                   Position = Points[0];
                    
                   var localPath = new List<System.Windows.Point>(Points.Count);
                   var offset = Map.FromLatLngToLocal(Points[0]);
                   foreach(var i in Points)
                   {
                      var p = Map.FromLatLngToLocal(i);
                      localPath.Add(new System.Windows.Point(p.X - offset.X, p.Y - offset.Y));
                   }
    
                   var shape = PathHelper.CreateRoutePath(localPath);

                   var path = Shape as Path;
                   if(path != null)
                   {
                      path.Data = shape.Data;
                   }
                   else
                   {
                      Shape = shape;
                   }
                }
                else
                {
                   Shape = null;
                }
            }
        }
    }
}
