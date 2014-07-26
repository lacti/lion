using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LionLibrary;

namespace Lion
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 1)
            {
                // register shell contextmenu
                if (MessageBox.Show("Shell ContextMenu에 Lion을 등록하시겠습니까?", "Lion", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    FileShellExtension.Register("*", "lion", "Execute Lion",
                        string.Format("\"{0}\" \"%1\"", Application.ExecutablePath));
                    MessageBox.Show("등록되었습니다.\n변환할 파일에서 우클릭한 후 Execute Lion을 실행해주세요.", "Lion", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // execute converting works
                var targetFiles = args.Skip(1).ToList();
                var targetExts = targetFiles.Select(Path.GetExtension).Select(e => e.ToLower()).ToArray();

                // check input files' integrity
                var isXmlTarget = targetExts.All(e => e == ".xml");
                var isExcelTarget = targetExts.All(e => e == ".xls") || targetExts.All(e => e == ".xlsx");
                if ((!isXmlTarget && !isExcelTarget) || (isXmlTarget && isExcelTarget))
                {
                    MessageBox.Show("변환할 파일 형태는 모두 xml이거나 모두 xls(x) 파일이어야 합니다.\nxls(x) 파일을 변환할 때에는 같은 위치에 원본 xml 파일이 있어야 합니다.",
                        "Lion", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var worker = new StringWorker();
                if (isXmlTarget)
                {
                    // inference xml-schema
                    var schema = new StringXmlSchema();
                    foreach (var target in targetFiles)
                        schema.BuildFromXml(target);

                    // edit xml-schema for finding value attribute
                    if ((new FormSchemaEditor(schema)).ShowDialog() == DialogResult.Cancel)
                        return;

                    // export string entries from xml to excel
                    var entries = worker.LoadFromXml(schema, targetFiles);
                    var outputPath = targetFiles.Count == 1
                        ? Path.Combine(Path.GetDirectoryName(targetFiles[0]), Path.GetFileNameWithoutExtension(targetFiles[0]) + ".xlsx")
                        : Path.Combine(Path.GetDirectoryName(targetFiles[0]), "Strings-" + DateTime.Now.ToString("yyMMddHHmmss") + ".xlsx");
                    worker.SaveToExcel(entries, outputPath, true);
                }
                else if (isExcelTarget)
                {
                    // load all of string entries from excel files
                    var entries = new List<StringEntry>();
                    foreach (var target in targetFiles)
                        entries.AddRange(worker.LoadFromExcel(target));

                    // export string entries from excel to xml
                    var refXmlFiles = Directory.GetFiles(Path.GetDirectoryName(targetFiles[0]), "*.xml");
                    worker.SaveToXml(entries, refXmlFiles);
                }

                // show working report
                Application.Run(new FormReporter(worker.Reporter));
            }
        }
    }
}
