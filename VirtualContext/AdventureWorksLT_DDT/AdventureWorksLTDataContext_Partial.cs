using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Schrodinger.Diagnostics;
using System.IO;

namespace AdventureWorksLT
{
    public partial class AdventureWorksLTDataContext
    {
        partial void OnCreated()
        {
            TextWriter log = new DebuggerWriter();
            log.WriteLine("%%%%% New DataContext Created. %%%%%");
            this.Log = log;
        } 
    }
}
