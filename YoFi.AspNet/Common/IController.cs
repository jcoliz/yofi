using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.AspNet
{
    /// <summary>
    /// Standard ASP.NET controller functionality
    /// </summary>
    /// <remarks>
    /// User for standard controller unit tests operating on type <typeparamref name="T"/>
    /// </remarks>
    public interface IController<T>
    {
        Task<IActionResult> Index();
        Task<IActionResult> Details(int? id);
        Task<IActionResult> Edit(int? id);
        Task<IActionResult> Edit(int id, T item);
        Task<IActionResult> Create(T item);
        Task<IActionResult> Delete(int? id);
        Task<IActionResult> DeleteConfirmed(int id);
        Task<IActionResult> Download();
        Task<IActionResult> Upload(List<IFormFile> files);
    }
}
