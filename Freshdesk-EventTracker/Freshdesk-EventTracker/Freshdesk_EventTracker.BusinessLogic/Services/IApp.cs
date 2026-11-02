using System;
using System.Collections.Generic;
using System.Text;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public interface IApp
    {
       Task RunAppAsync(string apiKey,
            string freshdeskUrl,
            string proffixUrl,
            string proffixUser,
            string proffixPassword,
            string database);

      
    }
}
