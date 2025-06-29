using Autodesk.Revit.DB;

namespace Auto_Sheets.Models
{
    public class TitleBlockModel
    {
        public string Name { get; set; }
        public ElementId Id { get; set; }

        public override string ToString() => Name;
    }
}
