using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFunctionAppDemo
{
    public class ParseExcelFileData
    {
        public static async Task<List<FileDataPayload>> GetFileData(Stream stream, ILogger log)
        {
            var fileData = new List<FileDataPayload>();
            var text = string.Empty;
            var index = -1;
            var columnRef = string.Empty;
            var success = false;

            //parse the sheet
            using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(stream, false))
            {
                var workbookPart = spreadsheetDocument.WorkbookPart;

                var worksheetPart = workbookPart.WorksheetParts.First();
                var sstpart = workbookPart.GetPartsOfType<SharedStringTablePart>().First();
                var sst = sstpart.SharedStringTable;
                var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                var rowIndex = 0;

                //parse out trainee data including name, email, course(s) and codes.
                foreach (var r in sheetData.Elements<Row>())
                {
                    var fdpd = new FileDataPayload();

                    foreach (var c in r.Elements<Cell>())
                    {
                        if (string.IsNullOrWhiteSpace(c?.CellValue?.Text)) continue;

                        success = int.TryParse(c.CellValue.Text, out int cellIndex);
                        if (success)
                        {
                            text = sst?.ChildElements[cellIndex]?.InnerText ?? "";
                        }
                        else
                        {
                            text = c?.CellValue?.Text ?? "";
                        }

                        log.LogInformation($"RowIndex: {rowIndex}\t|CellIndex: {cellIndex}\t|Text: {text}");

                        columnRef = c?.CellReference?.Value?.Substring(0, 1) ?? "";
                        if (string.IsNullOrWhiteSpace(columnRef)) continue;
                        success = int.TryParse(c.CellReference.Value.Substring(1, 1), out index);
                        if (!success) break;
                        if (index == 1)
                        {
                            break;
                        }

                        switch (columnRef?.ToUpper())
                        {
                            case "A":
                                //UniqueId
                                fdpd.UniqueId = text;
                                break;
                            case "B":
                                //Name
                                fdpd.Name = text;
                                break;
                            case "C":
                                //email
                                fdpd.Email = text;
                                break;
                            default:
                                //ignore
                                break;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(fdpd.UniqueId))
                    {
                        log.LogInformation($"adding {fdpd.UniqueId} - {fdpd.Name} - {fdpd.Email}");
                        fileData.Add(fdpd);
                    }
                    else
                    {
                        log.LogInformation($"Row [{rowIndex}] skipped for invalid/no data");
                    }
                    rowIndex++;
                }
            }

            return fileData;
        }
    }

    public class FileDataPayload
    { 
        public string Name { get; set; }
        public string Email { get; set; }
        public string UniqueId { get; set; }
    }
}
