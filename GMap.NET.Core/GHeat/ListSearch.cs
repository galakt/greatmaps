using System;
using System.Collections.Generic;

namespace GMap.NET.GHeat
{
   /// <summary>
   /// This is my attempt to divide and conquer when searching the list. 
   /// Testing this has shown that is actually takes longer to execute vs just using linq 
   /// not sure if it is my coding or just a fact of life.
   /// </summary>
   public class ListSearch
   {
      private PointLatLng _topLeftBound;
      private PointLatLng _lowerRightBound;
      private List<PointLatLng> _list;
      private List<System.Threading.Thread> _threadTracking;
      private Object _threadLocker = new Object();
      private List<PointLatLng> _returnList;

      public class ListInstructions
      {
         public int startSearchIndex;
         public int endSearchIndex;
      }

      public ListSearch(List<PointLatLng> list, PointLatLng topLeftBound, PointLatLng lowerRightBound)
      {
         _topLeftBound = topLeftBound;
         _lowerRightBound = lowerRightBound;
         _list = list;
         _threadTracking = new List<System.Threading.Thread>();
         _returnList = new List<PointLatLng>();
      }

      public IEnumerable<PointLatLng> GetMatchingPoints()
      {
         System.Threading.Thread newThread;
         int split = _list.Count / Environment.ProcessorCount;
         List<PointLatLng> tempList = new List<PointLatLng>();

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
         List<PointLatLng> tempList = new List<PointLatLng>();
         PointLatLng point;

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
