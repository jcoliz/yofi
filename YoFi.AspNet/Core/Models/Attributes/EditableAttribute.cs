using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Models.Attributes
{
    // Marks that a property is editable in our model sytem
    [AttributeUsage(AttributeTargets.Property)]
    public class EditableAttribute : Attribute
    {
        public EditableAttribute()
        {
        }
    }   
}
