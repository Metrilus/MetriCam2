// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System.Reflection;
using System.Resources;

namespace MetriCam2
{
    public class Localization
    {
        private static ResourceManager instance = null;
        
        public static ResourceManager Instance
        {
            get
            {
                if (null == instance)
                {
                    Assembly metriCamAssembly = Assembly.Load("MetriCam2");
                    instance = new ResourceManager("MetriCam2.MetriCamLocale", metriCamAssembly);
                }
                return instance;
            }
        }

        public static string GetString(string name)
        {
            return Instance.GetString(name);
        }
    }
}
