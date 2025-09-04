using Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Loader
{
    public class HotLoader : MonoBehaviour
    {
        private static int currentIndex = 0;
        Queue<string> strings = new Queue<string>();

        public void FixedUpdate()
        {
            if (strings.Count == 0)
            {
                strings = new Queue<string>(Templates.textureCache.Keys);

            }
            // Only load one image per FixedUpdate
            if (strings.Count > 0)
            {
                Templates.textureCache[strings.Dequeue()].LoadImage(false);
            }
            currentIndex++;

            if (currentIndex >= Templates.textureCache.Count)
            {
                currentIndex = 0;
            }
        }


    }
}
