namespace GMap.NET.GHeat
{
   /// <summary>
   /// Interface used as a type of event listner.
   /// </summary>
   public interface IWeightHandler
   {
      /// <summary>
      /// Evaluates the data and returns it's weight
      /// </summary>
      /// <param name="data"></param>
      /// <returns></returns>
      decimal Evaluate(object data);
   }
}