using System;
using System.Windows.Forms;

namespace MetaMe.WindowsClient
{
    class ClientApplicationContext : ApplicationContext
    {
        void Exit(object sender, EventArgs e)
        {
            if (Application.MessageLoop)
            {
                Application.Exit();
            }
            else
            {
                Environment.Exit(1);
            }
        }
    }
}
