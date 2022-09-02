using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Core
{
    class Device
    {
        //if device guid doesn't exist, create, then return. else return
        public static Guid GetDeviceGuid(bool devMode)
        {
            string dataPath = PathUtils.GetDeviceIdPath(devMode);

            if (!File.Exists(dataPath))
            {
                throw new Exception("DeviceId does not exist");
            }

            string contents = File.ReadAllText(dataPath);
            return Guid.Parse(contents);
        }
    }
}
