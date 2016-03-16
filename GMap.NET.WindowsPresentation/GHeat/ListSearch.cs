using System;
using System.Collections.Generic;
using System.Linq;

namespace GMap.NET.WindowsPresentation.GHeat
{
   /// <summary>
   /// This is my attempt to divide and conquer when searching the list. 
   /// Testing this has shown that is actually takes longer to execute vs just using linq 
   /// not sure if it is my coding or just a fact of life.
   /// </summary>
   public class ListSearch
   {
      private GMap.NET.PointLatLng _topLeftBound;
      private GMap.NET.PointLatLng _lowerRightBound;
      List<GMap.NET.PointLatLng> _list;
      private List<System.Threading.Thread> _threadTracking;
      private Object _threadLocker = new Object();
      private List<GMap.NET.PointLatLng> _returnList;

      public class ListInstructions
      {
         public int startSearchIndex;
         public int endSearchIndex;
      }

      public ListSearch(List<GMap.NET.PointLatLng> list, GMap.NET.PointLatLng topLeftBound, GMap.NET.PointLatLng lowerRightBound)
      {
         _topLeftBound = topLeftBound;
         _lowerRightBound = lowerRightBound;
         _list = list;
         _threadTracking = new List<System.Threading.Thread>();
         _returnList = new List<GMap.NET.PointLatLng>();
      }

      public IEnumerable<GMap.NET.PointLatLng> GetMatchingPoints()
      {
         System.Threading.Thread newThread;
         int split = _list.Count() / Environment.ProcessorCount;
         List<GMap.NET.PointLatLng> tempList = new List<GMap.NET.PointLatLng>();

         //Create a thread for every processor on the computer
         for (int i = 0; i < Environment.ProcessorCount; i++)
         {
            newThread = new System.Threading.Thread(DivideAndConquer);
            newThread.Start(new ListInstructions() { startSearchIndex = i * split, endSearchIndex = i * split + split });
            _threadTracking.Add(newThread);
         }

         //Wait for each thread to complete searching
         foreach (System.Threading.Thread thread in _threadTracking)
            thread.Join();

         return _returnList;
      }

      private void DivideAndConquer(object instructions)
      {
         ListInstructions listInstruction = (ListInstructions)instructions;
         List<GMap.NET.PointLatLng> tempList = new List<GMap.NET.PointLatLng>();
         GMap.NET.PointLatLng point;

         for (int i = listInstruction.startSearchIndex; i < listInstruction.endSearchIndex; i++)
         {
            point = _list[i];
            if (point.Lat <= _topLeftBound.Lat && point.Lng >= _topLeftBound.Lng && point.Lat >= _lowerRightBound.Lat && point.Lng <= _lowerRightBound.Lng)
               tempList.Add(point);
         }

         lock (_threadLocker)
         {
            _returnList.AddRange(tempList.ToArray());
         }
      }
   }
}
