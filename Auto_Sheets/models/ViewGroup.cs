using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auto_Sheets.Models
{
    public class ViewGroup
    {
        public string GroupName { get; set; }
        public string GroupNumber { get; set; }

        public List<ViewModel> Views { get; set; } = new List<ViewModel>();
        public string GroupDisplay => $"{GroupNumber} - {GroupName}";
    }
}
