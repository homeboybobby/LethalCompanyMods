using UnityEngine;

namespace PhantomMode.Patches
{
    public class Common
    {

        public static bool ArePointsClose(Vector3 self, Vector3 other, float threshold)
        {
            float dist = Vector3.Distance(self, other);
            if (dist <= threshold) return true;
            return false;
        }
    }
}