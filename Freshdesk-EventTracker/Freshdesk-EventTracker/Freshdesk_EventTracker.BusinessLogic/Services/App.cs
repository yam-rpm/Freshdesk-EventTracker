using System;
using System.Collections.Generic;
using System.Text;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public class App : IApp
    {
        public Task RunAppAsync() {
            Console.WriteLine("Test");
            return Task.CompletedTask;
        }
    }
}
