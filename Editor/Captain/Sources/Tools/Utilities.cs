using System;

namespace UnityEditor.PackageManager.UI.Captain
{
    internal static class Utilities
    {
        private static bool IsSameOrSubclass(Type potentialBase, Type potentialDescendant)
        {
            return potentialDescendant.IsSubclassOf(potentialBase)
                   || potentialDescendant == potentialBase;
        }
        
        public static bool IsSameOrSubclass<T>(object potentialBase) where T: new()
        {
            return IsSameOrSubclass(potentialBase.GetType(), typeof(T));
        }
    }
}