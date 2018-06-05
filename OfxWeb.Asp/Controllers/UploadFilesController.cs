using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using OfxSharpLib;

namespace OfxWeb.Asp.Controllers
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-2.1

    public class UploadFilesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("UploadFiles")]
        public async Task<IActionResult> Post(List<IFormFile> files)
        {
            try
            {
                long size = files.Sum(f => f.Length);

                foreach (var formFile in files)
                {
                    using (var stream = formFile.OpenReadStream())
                    {
                        var parser = new OfxDocumentParser();
                        var Document = parser.Import(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex); 
            }

            return Ok();
        }
    }
}