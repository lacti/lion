using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LionLibrary;

namespace LionConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            const string testFile = @"C:\Users\lacti\Desktop\str.xml";
            var schema = new StringXmlSchema();
            schema.Load(@"C:\Users\lacti\Desktop\str-dev.xml");

            var worker = new StringWorker();
            var entries = worker.LoadFromXml(schema, new[] { testFile });
            worker.SaveToExcel(entries, @"C:\Users\lacti\Desktop\str.xlsx", true);
            worker.SaveToXml(worker.LoadFromExcel(@"C:\Users\lacti\Desktop\str2.xlsx"), new [] { @"C:\Users\lacti\Desktop\str.xml" });
        }
    }
}
