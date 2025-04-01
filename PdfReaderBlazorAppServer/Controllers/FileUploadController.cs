using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;
using SixLabors.ImageSharp;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using PdfReaderBlazorAppServer.Models;

namespace PdfReaderBlazorAppServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не загружен.");

            string extractedText;

            if (file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                extractedText = await ReadPdfText(file);
            }
            else
            {
                extractedText = await ReadImageText(file);
            }

            var extractedFields = ExtractFields(extractedText);

            return Ok(new
            {
                Text = extractedText,
                Data = extractedFields
            });
        }

        private async Task<string> ReadPdfText(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var pdfDocument = PdfDocument.Open(memoryStream);
            var sb = new StringBuilder();

            foreach (var page in pdfDocument.GetPages())
            {
                sb.AppendLine(page.Text);
            }

            return sb.ToString();
        }

        private async Task<string> ReadImageText(IFormFile file)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

            using (var image = await Image.LoadAsync(file.OpenReadStream()))
            {
                await image.SaveAsPngAsync(tempFile);
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = @"C:\Tools\Tesseract\tesseract.exe", 
                Arguments = $"\"{tempFile}\" stdout -l fra",  
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            string result = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            System.IO.File.Delete(tempFile);

            return result;
        }

        private ParsedDocumentData ExtractFields(string text)
        {
            var data = new ParsedDocumentData();

            data.Nom = ExtractField(text, @"(?<=Nom\s?:?\s?)(.*?)(?=\s+Date)");
            data.Date = ExtractField(text, @"(?<=Date\s?:?\s?)\d{2}/\d{2}/\d{4}");
            data.Montant = ExtractField(text, @"(?<=Montant\s?:?\s?)\d+([.,]\d{2})?\s?€?");
            data.Numero = ExtractField(text, @"(?<=Numero\s?:?\s?)[^\r\n]+");

            return data;
        }

        private string? ExtractField(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.Multiline);
            return match.Success ? match.Value.Trim() : null;
        }
    }
}