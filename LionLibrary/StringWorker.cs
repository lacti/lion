using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ClosedXML.Excel;
using System.IO;

namespace LionLibrary
{
    public class StringWorker
    {
        private readonly LionReporter _reporter = new LionReporter();

        public LionReporter Reporter { get { return _reporter; } }

        #region helper

        private IEnumerable<string> GetXmlFiles(IEnumerable<string> xmlPaths)
        {
            foreach (var xmlPath in xmlPaths)
            {
                _reporter.Log(ReportEvent.InputXmlPath, xmlPath);

                if (File.Exists(xmlPath))
                    yield return xmlPath;
                else if (Directory.Exists(xmlPath))
                {
                    foreach (var xmlFile in Directory.GetFiles(xmlPath, "*.xml", SearchOption.AllDirectories))
                        yield return xmlFile;
                }
                else
                {
                    _reporter.Log(ReportEvent.CannotFindFile, xmlPath);
                }
            }
        }

        #endregion

        #region xml

        public List<StringEntry> LoadFromXml(StringXmlSchema schema, IEnumerable<string> xmlPaths)
        {
            _reporter.Log(ReportEvent.SchemaName, schema.Name);

            var entries = new List<StringEntry>();
            foreach (var xmlFile in GetXmlFiles(xmlPaths))
            {
                _reporter.Log(ReportEvent.XmlFilePath, xmlFile);

                try
                {
                    var xml = XElement.Load(xmlFile);
                    LoadFromXml(entries, xmlFile, schema.Root.Children[xml.Name.LocalName], xml);
                }
                catch (XmlException e)
                {
                    _reporter.Log(ReportEvent.CannotParseXml, e, xmlFile);
                }
            }
            _reporter.Log(ReportEvent.LoadCount, entries.Count.ToString());
            return entries;
        }

        private void LoadFromXml(List<StringEntry> entries, string xmlPath, StringXmlSchema.Node schemaNode, XElement element)
        {
            foreach (var childElement in element.Elements())
            {
                StringXmlSchema.Node childSchemaNode;
                if (!schemaNode.Children.TryGetValue(childElement.Name.LocalName, out childSchemaNode))
                    continue;
                LoadFromXml(entries, xmlPath, childSchemaNode, childElement);
            }

            foreach (var attribute in element.Attributes())
            {
                StringXmlSchema.AttributeType attributeType;
                if (!schemaNode.Attributes.TryGetValue(attribute.Name.LocalName, out attributeType))
                    continue;

                if (attributeType != StringXmlSchema.AttributeType.String)
                    continue;

                var stringValue = attribute.Value;
                if (string.IsNullOrWhiteSpace(stringValue))
                    continue;

                entries.Add(new StringEntry
                {
                    FileName = Path.GetFileName(xmlPath),
                    Address = element.GetAbsoluteXPath() + "/@" + attribute.Name.LocalName,
                    Original = stringValue,
                    Translated = string.Empty
                });
            }
        }

        public void SaveToXml(IEnumerable<StringEntry> entries, string[] xmlFiles)
        {
            var outputPath = Path.Combine(Path.GetDirectoryName(xmlFiles.First()), "output");
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
            _reporter.Log(ReportEvent.OutputPath, outputPath);

            var writeCount = 0;
            var entriesArray = entries as StringEntry[] ?? entries.ToArray();
            foreach (var xmlFile in xmlFiles)
            {
                _reporter.Log(ReportEvent.XmlFilePath, xmlFile);

                var fileName = Path.GetFileName(xmlFile);
                XDocument document;
                try
                {
                    document = XDocument.Load(xmlFile);
                }
                catch (XmlException e)
                {
                    _reporter.Log(ReportEvent.CannotParseXml, e, xmlFile);
                    continue;
                }
                writeCount += WriteEntriesToXml(document,
                    entriesArray.Where(e => e.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)));
                try
                {
                    var outputFile = Path.Combine(outputPath, fileName);
                    _reporter.Log(ReportEvent.OutputFile, outputFile);

                    document.Save(outputFile);
                }
                catch (XmlException e)
                {
                    _reporter.Log(ReportEvent.CannotSaveXml, e, xmlFile);
                    continue;
                }
            }
            _reporter.Log(ReportEvent.WriteCount, writeCount.ToString());
        }

        private int WriteEntriesToXml(XDocument document, IEnumerable<StringEntry> entries)
        {
            var writeCount = 0;
            foreach (var entry in entries)
            {
                var attributePos = entry.Address.LastIndexOf('/');
                if (attributePos < 0)
                {
                    _reporter.Log(ReportEvent.InvalidAddress, entry.Address);
                    continue;
                }

                var xpath = entry.Address.Substring(0, attributePos);
                var attributeName = entry.Address.Substring(attributePos + 2 /* /@ */);
                var element = document.XPathSelectElement(xpath);
                if (element != null)
                {
                    var attribute = element.Attribute(attributeName);
                    if (attribute != null)
                    {
                        var original = attribute.Value;
                        if (original == entry.Original)
                        {
                            attribute.Value = entry.Translated;
                            ++writeCount;
                        }
                        else
                        {
                            _reporter.Log(ReportEvent.ValueMismatched, string.Format("expect [{0}] <-> actual [{1}]",
                                entry.Original, original));
                        }
                    }
                    else
                    {
                        _reporter.Log(ReportEvent.NoAttributeFound, entry.Address);
                    }
                }
                else
                {
                    _reporter.Log(ReportEvent.NoElementFound, entry.Address);
                }
            }
            return writeCount;
        }

        #endregion

        #region excel

        public List<StringEntry> LoadFromExcel(string excelPath)
        {
            _reporter.Log(ReportEvent.InputExcelPath, excelPath);
            var entries = new List<StringEntry>();
            try
            {
                using (var workbook = new XLWorkbook(excelPath))
                {
                    foreach (var worksheet in workbook.Worksheets.Where(e => e.Visibility == XLWorksheetVisibility.Visible))
                    {
                        var lastRow = worksheet.LastRowUsed().RowNumber();
                        for (var row = 1; row <= lastRow; ++row)
                        {
                            entries.Add(new StringEntry
                            {
                                FileName = worksheet.Cell(row, 1).GetValue<string>(),
                                Address = worksheet.Cell(row, 2).GetValue<string>(),
                                Original = worksheet.Cell(row, 3).GetValue<string>(),
                                Translated = worksheet.Cell(row, 4).GetValue<string>(),
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _reporter.Log(ReportEvent.CannotOpenExcel, e, excelPath);
            }
            _reporter.Log(ReportEvent.LoadCount, entries.Count.ToString());
            return entries;
        }

        public void SaveToExcel(IEnumerable<StringEntry> entries, string excelPath, bool fileGroup)
        {
            var writeCount = 0;
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    if (fileGroup)
                    {
                        foreach (var pair in entries.GroupBy(e => Path.GetFileNameWithoutExtension(e.FileName)))
                        {
                            writeCount += WriteEntriesToExcel(workbook, pair, pair.Key);
                        }
                    }
                    else
                    {
                        writeCount += WriteEntriesToExcel(workbook, entries, "L10N");
                    }
                    workbook.SaveAs(excelPath);
                }
            }
            catch (Exception e)
            {
                _reporter.Log(ReportEvent.CannotSaveExcel, e, excelPath);
            }
            _reporter.Log(ReportEvent.WriteCount, writeCount.ToString());
            _reporter.Log(ReportEvent.OutputFile, excelPath);
        }

        private int WriteEntriesToExcel(XLWorkbook workbook, IEnumerable<StringEntry> entries, string sheetName)
        {
            var writeCount = 0;
            var worksheet = workbook.Worksheets.Add(sheetName);
            var row = 1;
            foreach (var entry in entries)
            {
                worksheet.Cell(row, 1).Value = entry.FileName;
                worksheet.Cell(row, 2).Value = entry.Address;
                worksheet.Cell(row, 3).Value = entry.Original;
                worksheet.Cell(row, 4).Value = entry.Translated;
                ++row;
                ++writeCount;
            }
            worksheet.Columns(1, 2).Hide();
            worksheet.Columns(3, 4).AdjustToContents();
            return writeCount;
        }

        #endregion
    }
}
