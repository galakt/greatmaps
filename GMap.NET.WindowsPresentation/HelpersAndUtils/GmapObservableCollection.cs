using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace GMap.NET.WindowsPresentation.HelpersAndUtils
{
   public class GmapObservableCollection<T> : ObservableCollection<T>
   {
      // http://stackoverflow.com/questions/224155/when-clearing-an-observablecollection-there-are-no-items-in-e-olditems
      protected override void ClearItems()
      {
         CheckReentrancy();
         var items = Items.ToList();
         base.ClearItems();
         OnPropertyChanged(new PropertyChangedEventArgs(CountString));
         OnPropertyChanged(new PropertyChangedEventArgs(IndexerName));
         OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, -1));
      }

      private const string CountString = "Count";
      private const string IndexerName = "Item[]";
   }
}
