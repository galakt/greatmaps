using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using GMap.NET.WindowsPresentation.Interfaces;

namespace GMap.NET.WindowsPresentation.HelpersAndUtils
{
   public class GmapObservableCollection<T> : ObservableCollection<T>
   {
      protected override void ClearItems()
      {
         foreach (var item in Items)
         {
            var clearableItem = item as IClearable;
            if (clearableItem != null)
            {
               clearableItem.Clear();
            }
         }
         base.ClearItems();
      }
   }
}
