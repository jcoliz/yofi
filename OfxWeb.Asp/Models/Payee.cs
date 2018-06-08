using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public class Payee
    {
        [Key]
        public string Name { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
    }
}
