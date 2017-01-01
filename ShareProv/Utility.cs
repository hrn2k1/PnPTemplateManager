using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareProv
{
    public class LoginEventArgs
    {
        public string SiteUrl { get; set; }
        public string UserName { get; set; }
        public ClientContext Context { get; set; }
    }
    /// <summary>
    /// Test
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void LoggedInEventHandler(object sender, LoginEventArgs e);

}
