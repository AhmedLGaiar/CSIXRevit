using Autodesk.Revit.DB;

namespace Auto_Sheets.Models
{
    public class ViewModel
    {
        public string ViewName { get; set; }
        public string ViewType { get; set; }
        public ElementId ViewId { get; set; }

        public string DisplayName => $"{ViewType} : {ViewName}";
    }
}
